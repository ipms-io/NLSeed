// using System.Net;
// using System.Net.Sockets;
// using System.Text;
// using Microsoft.Extensions.Logging;
// using NLSeed.DnsModels;
// using NLSeed.Encoders;
// using NLSeed.Models;
// using NLSeed.Parsers;
//
// namespace NLSeed.Services;
//
// public class DnsService(ILogger<DnsService> logger, NetworkViewService networkViewService)
// {
//     public async Task RunUdpServerAsync(CancellationToken cancellationToken)
//     {
//         // Create a UDP client bound to port 53.
//         using var udpClient = new UdpClient(53);
//         logger.LogInformation("Udp Server running on port 53");
//
//         while (!cancellationToken.IsCancellationRequested)
//         {
//             try
//             {
//                 // Wait for an incoming DNS query.
//                 var result = await udpClient.ReceiveAsync(cancellationToken);
//                 var remoteEndPoint = result.RemoteEndPoint;
//                 var requestBytes = result.Buffer;
//                 
//                 // Log the query.
//                 logger.LogTrace("Received DNS query from {remoteEndPoint}", remoteEndPoint);
//                 logger.LogTrace("Received {receivedCount} bytes from {remoteEP}", requestBytes.Length, remoteEp);
//                 
//                 try
//                 {
//                     var dnsMessage = DnsMessage.Parse(requestBytes);
//                     var request = DnsRequestParser.ParseRequest(dnsMessage.Questions[0].QName, dnsMessage.Questions[0].QType);
//                     var response = CreateResponse(request);
//                     var responseBytes = response.ToByteArray();
//                     await udpClient.SendAsync(responseBytes, responseBytes.Length, remoteEp);
//                     logger.LogTrace("Response sent to {remoteEP}", remoteEp);
//                 }
//                 catch (Exception e)
//                 {
//                     logger.LogError(e, "Parsing/Response error");
//                 }
//             }
//             catch (SocketException ex)
//             {
//                 logger.LogError(ex, "Socket exception!");
//                 // Optionally, handle socket errors or break the loop.
//             }
//             catch (Exception ex)
//             {
//                 logger.LogError(ex, "Error!");
//             }
//         }
//     }
//
//     private DnsMessage CreateResponse(DnsMessage request)
//     {
//         var nodes = networkViewService.RandomSample(255, 25);
//         
//         var header = new DnsRRHeader
//         {
//             Name = request.Question[0].Name,
//             Type = DnsType.SRV,
//             Class = DnsClass.INET,
//             Ttl = 60
//         };
//         
//         subDomain = subDomain.Trim();
//         var segments = subDomain.Split('.', StringSplitOptions.RemoveEmptyEntries);
//         var prefix = segments.Length switch
//         {
//             4 or 3 => segments[2],
//             2 => segments[0],
//             _ => ""
//         };
//
//         foreach (var n in nodes)
//         {
//             string encodedId;
//             try
//             {
//                 encodedId = new Bech32Encoder().EncodeNodeId(n.Id);
//             }
//             catch (Exception e)
//             {
//                 logger.LogError(e, "Unable to decode id {n.Id}", n.Id);
//                 continue;
//             }
//
//             // Build the node name in the form: <encodedId>.<prefix><rootDomain>.
//             var nodeName = $"{encodedId}.{prefix}ipms.io.";
//
//             // Create the SRV record with hard-coded Priority and Weight.
//             var rr = new DnsSRVRecord
//             {
//                 Header = header,
//                 Priority = 10,
//                 Weight = 10,
//                 Target = nodeName,
//                 Port = (ushort)n.Addresses.First().Port
//             };
//
//             response.Answer.Add(rr);
//             // Optionally add additional A/AAAA records based on n.Type.
//         }
//     }
//
//     private bool IsSrvQuery(byte[] data)
//     {
//         // Basic check: assume DNS header is 12 bytes, then question.
//         // Skip QNAME: it is a series of labels terminated by a 0 byte.
//         try
//         {
//             var pos = 12;
//             while (data[pos] != 0)
//             {
//                 // Each label is prefixed by its length.
//                 pos += data[pos] + 1;
//             }
//             pos++; // Skip the null label terminator.
//             // QTYPE: 2 bytes, then QCLASS: 2 bytes.
//             var qType = (ushort)((data[pos] << 8) | data[pos + 1]);
//             // SRV records have QTYPE = 33.
//             return qType == 33;
//         }
//         catch
//         {
//             return false;
//         }
//     }
//     
//     private byte[] BuildDnsName(string domain)
//     {
//         // Convert "server.example.com" to DNS format:
//         // <length>server<length>example<length>com<0>
//         var labels = domain.Split('.');
//         var result = new List<byte>();
//         foreach (var label in labels)
//         {
//             result.Add((byte)label.Length);
//             result.AddRange(Encoding.ASCII.GetBytes(label));
//         }
//         result.Add(0); // null terminator for the domain name.
//         return result.ToArray();
//     }
//     
//     private byte[] BuildSrvRData()
//     {
//         // Build RDATA for an SRV record:
//         // Priority (2 bytes) = 0
//         // Weight (2 bytes) = 0
//         // Port (2 bytes) = 1234 (0x04D2)
//         // Target = "server.example.com" in DNS name format.
//         var target = BuildDnsName("server.example.com");
//         var rdata = new byte[6 + target.Length];
//         // Priority: 0
//         rdata[0] = 0x00; rdata[1] = 0x00;
//         // Weight: 0
//         rdata[2] = 0x00; rdata[3] = 0x00;
//         // Port: 1234
//         rdata[4] = 0x04; rdata[5] = 0xD2;
//         // Copy target
//         Array.Copy(target, 0, rdata, 6, target.Length);
//         return rdata;
//     }
//     
//     private int GetQuestionLength(byte[] data)
//     {
//         // Starting at offset 12, skip over QNAME, then 1 byte (0 terminator),
//         // then QTYPE (2 bytes) and QCLASS (2 bytes)
//         var pos = 12;
//         while (data[pos] != 0)
//         {
//             pos += data[pos] + 1;
//         }
//         pos++; // Skip the 0 terminator.
//         pos += 4; // QTYPE and QCLASS.
//         return pos - 12;
//     }
//     
//     private byte[] BuildSrvResponse(byte[] queryData)
//     {
//         // Build a minimal DNS response:
//         // - Copy header from query, then modify flags to indicate a response.
//         // - Copy question section.
//         // - Append one answer record for an SRV record.
//         // 
//         // This sample hard-codes:
//         //   SRV RDATA: Priority=0, Weight=0, Port=1234, Target="server.example.com"
//
//         // We'll build the response into a buffer.
//         var buffer = new byte[512];
//         // Copy header (first 12 bytes)
//         Array.Copy(queryData, 0, buffer, 0, 12);
//         // Set response flag (bit 15) and no error.
//         // For simplicity, set flags to 0x8180.
//         buffer[2] = 0x81;
//         buffer[3] = 0x80;
//         // Set Answer Count to 1 (bytes 6-7).
//         buffer[6] = 0x00;
//         buffer[7] = 0x01;
//
//         // Determine the length of the question section.
//         var questionLength = GetQuestionLength(queryData);
//         // Copy question section
//         Array.Copy(queryData, 12, buffer, 12, questionLength);
//         var pos = 12 + questionLength;
//
//         // Answer record:
//         // Name: pointer to offset 12 (0xC00C)
//         buffer[pos++] = 0xC0;
//         buffer[pos++] = 0x0C;
//         // TYPE: SRV (33) => 0x00 0x21
//         buffer[pos++] = 0x00;
//         buffer[pos++] = 0x21;
//         // CLASS: IN (1) => 0x00 0x01
//         buffer[pos++] = 0x00;
//         buffer[pos++] = 0x01;
//         // TTL: 300 seconds (0x0000012C)
//         buffer[pos++] = 0x00;
//         buffer[pos++] = 0x00;
//         buffer[pos++] = 0x01;
//         buffer[pos++] = 0x2C;
//         // RDLENGTH: will be length of our RDATA
//         var rdata = BuildSrvRData();
//         buffer[pos++] = (byte)(rdata.Length >> 8);
//         buffer[pos++] = (byte)(rdata.Length & 0xFF);
//         // Copy RDATA
//         Array.Copy(rdata, 0, buffer, pos, rdata.Length);
//         pos += rdata.Length;
//
//         // Return the final response (trimmed to actual length)
//         var response = new byte[pos];
//         Array.Copy(buffer, response, pos);
//         return response;
//     }
// }