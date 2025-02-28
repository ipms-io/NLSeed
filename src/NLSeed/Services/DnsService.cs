using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NLSeed.Services;

using DnsModels;
using Handlers;

public class DnsService(ILogger<DnsService> logger, IServiceProvider serviceProvider)
{
    public async Task RunUdpServerAsync(CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient(10053);
        logger.LogInformation("Udp Server running on port 53");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(cancellationToken);
                var remoteEndPoint = result.RemoteEndPoint;
                var requestBytes = result.Buffer;

                logger.LogTrace("Received {receivedCount} bytes from {remoteEP}", requestBytes.Length, remoteEndPoint);

                try
                {
                    var dnsMessage = DnsMessage.Parse(requestBytes);

                    using var scope = serviceProvider.CreateScope();
                    // TODO: Create Critical exception to halt the app
                    var lightningDnsHandler = scope.ServiceProvider.GetService<LightningDnsHandler>()
                                              ?? throw new Exception("Unable to get service from scope");
                    
                    var response = lightningDnsHandler.HandleLightningDns(dnsMessage);
                    await udpClient.SendAsync(response, response.Length, remoteEndPoint);
                    logger.LogTrace("Response sent to {remoteEP}", remoteEndPoint);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Parsing/Response error");
                }
            }
            catch (SocketException ex)
            {
                logger.LogError(ex, "Socket exception!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error!");
            }
        }
    }

    public async Task RunTcpServerAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, 10053);
        listener.Start();
        logger.LogInformation("TCP Server running on port 10053");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient tcpClient;
                try
                {
                    tcpClient = await listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Process each connection on a separate task.
                _ = Task.Run(async () =>
                {
                    using (tcpClient)
                    {
                        try
                        {
                            var stream = tcpClient.GetStream();

                            // Read the 2-byte length prefix.
                            var lenBytes = new byte[2];
                            await stream.ReadExactlyAsync(lenBytes, cancellationToken);
                            var queryLength = (ushort)((lenBytes[0] << 8) | lenBytes[1]);

                            // Read the DNS query bytes.
                            var queryBytes = new byte[queryLength];
                            await stream.ReadExactlyAsync(queryBytes, cancellationToken);

                            logger.LogTrace("TCP: Received {length} bytes", queryLength);

                            // Process the query.
                            var dnsMessage = DnsMessage.Parse(queryBytes);
                            
                            using var scope = serviceProvider.CreateScope();
                            // TODO: Create Critical exception to halt the app
                            var lightningDnsHandler = scope.ServiceProvider.GetService<LightningDnsHandler>()
                                                      ?? throw new Exception("Unable to get service from scope");
                            
                            var responseBytes = lightningDnsHandler.HandleLightningDns(dnsMessage);

                            // Prepare TCP DNS response: 2-byte length prefix in network order.
                            var responseLength = (ushort)responseBytes.Length;
                            var responseLenBytes = new byte[2]
                            {
                                (byte)(responseLength >> 8),
                                (byte)(responseLength & 0xFF)
                            };
                            await stream.WriteAsync(responseLenBytes.AsMemory(0, 2), cancellationToken);
                            await stream.WriteAsync(responseBytes, cancellationToken);
                            logger.LogTrace("TCP response sent.");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error handling TCP connection.");
                        }
                    }
                }, cancellationToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }
}