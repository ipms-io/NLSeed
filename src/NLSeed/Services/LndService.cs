using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NLSeed.Services;

using Interfaces;
using LnRpc;
using Models;

public class LndService : ILightningService
{
    private const int DefaultPort = 9735;
    
    private readonly string _macaroon;
    private readonly ILogger<LndService> _logger;
    private readonly Lightning.LightningClient _rpcClient;

    public LndService(IConfiguration configuration, ILogger<LndService> logger)
    {
        _logger = logger;

        var macaroonBytes = Convert.FromBase64String(configuration["Lnd:Macaroon"]
                                                     ?? throw new Exception("Lnd macaroon config is missing"));
        _macaroon = BitConverter.ToString(macaroonBytes).Replace("-", ""); // hex format stripped of "-" chars

        var rawCert = Convert.FromBase64String(configuration["Lnd:Cert"]
                                               ?? throw new Exception("Lnd certificate config is missing"));
        var x509Cert = new X509Certificate2(rawCert);
        var httpClientHandler = new HttpClientHandler
        {
            // Validating a self-signed cert won't work. Therefore, validate the certificate directly
            ServerCertificateCustomValidationCallback = (_, cert, _, _) => x509Cert.Equals(cert)
        };

        var credentials = ChannelCredentials.Create(new SslCredentials(), CallCredentials.FromInterceptor(AddMacaroon));

        var channel = GrpcChannel.ForAddress(
            configuration["Lnd:RpcAddress"] ?? throw new Exception("Lnd rpc address config is missing"),
            new GrpcChannelOptions
            {
                HttpHandler = httpClientHandler,
                Credentials = credentials,
                MaxReceiveMessageSize = 50 * 1024 * 1024
            });
        _rpcClient = new Lightning.LightningClient(channel);
    }

    public async Task<IDictionary<string, Node>> DescribeGraphAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = new Dictionary<string, Node>();
            var graphResponse = await _rpcClient.DescribeGraphAsync(new ChannelGraphRequest(), cancellationToken: cancellationToken);
            _logger.LogDebug("Got {nodeCount} nodes from lnd", graphResponse.Nodes.Count);
            
            foreach (var lightningNode in graphResponse.Nodes)
            {
                if (lightningNode.Addresses.Count == 0) continue;

                var node = new Node(lightningNode.PubKey);
                foreach (var nodeAddress in lightningNode.Addresses)
                {
                    var endpoint = await ParseEndpointAsync(nodeAddress.Addr, nodeAddress.Network, cancellationToken);
                    if (endpoint is null) continue;
                    
                    node.Addresses.Add(endpoint);
                    node.Type |= (byte)(endpoint.AddressFamily == AddressFamily.InterNetworkV6 ? 0x01 << 2 : 0x01);
                }

                if (node.Addresses.Count == 0) continue;
                
                response.TryAdd(node.Id, node);
            }

            return response;
        }
        catch (Exception e)
        {
            const string errorMessage = "Error describing graph";
            _logger.LogError(e, errorMessage);
            throw new Exception(errorMessage);
        }
    }

    public async Task<IDictionary<string, Node>> ListPeersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = new Dictionary<string, Node>();
            
            var peersResponse = await _rpcClient.ListPeersAsync(new ListPeersRequest(), cancellationToken: cancellationToken);
            _logger.LogInformation("Got {peerCount} peers from lnd", peersResponse.Peers.Count);

            foreach (var peer in peersResponse.Peers)
            {
                var node = new Node(peer.PubKey);
                
                var endpoint = await ParseEndpointAsync(peer.Address, cancellationToken: cancellationToken);
                if (endpoint is null) continue;
                    
                node.Addresses.Add(endpoint);
                node.Type |= (byte)(endpoint.AddressFamily == AddressFamily.InterNetworkV6 ? 0x01 << 2 : 0x01);
                response.TryAdd(node.Id, node);
            }
            
            return response;
        }
        catch (Exception e)
        {
            const string errorMessage = "Error listing peers";
            _logger.LogError(e, errorMessage);
            throw new Exception(errorMessage);
        }
    }

    private async Task<IPEndPoint?> ParseEndpointAsync(string address, string? network = null, CancellationToken cancellationToken = default)
    {
        // Prepend a dummy scheme to help with parsing.
        var parsedAsUri = Uri.TryCreate($"{network ?? "tcp"}://{address}", UriKind.Absolute, out var uri);

        if (!parsedAsUri || uri is null || uri.Host.EndsWith(".onion"))
            return null;
                    
        // Determine port: if none is specified, use the default.
        var port = uri.IsDefaultPort ? DefaultPort : uri.Port;

        // Get the host part from the URI.
        // For IPv6 addresses with port, the Uri class automatically strips the brackets.
        var host = uri.Host;
                    
        // Check if the host is already an IP address.
        if (IPAddress.TryParse(host, out var ipAddress)) 
            return new IPEndPoint(ipAddress, port);
        
        // Not an IP? Try to resolve it via DNS.
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (addresses == null || addresses.Length == 0)
            throw new Exception($"Unable to resolve host: {host}");
        
        // Use the first resolved address.
        ipAddress = addresses[0];

        return new IPEndPoint(ipAddress, port);
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _rpcClient.GetInfoAsync(new GetInfoRequest(), cancellationToken: cancellationToken);
            return response.SyncedToChain && response.SyncedToGraph;
        }
        catch (Exception e)
        {
            const string errorMessage = "Error fetching server info";
            _logger.LogError(e, errorMessage);
            throw new Exception(errorMessage);
        }
    }
    
    private Task AddMacaroon(AuthInterceptorContext context, Metadata metadata)
    {
        metadata.Add(new Metadata.Entry("macaroon", _macaroon));
        return Task.CompletedTask;
    }
}