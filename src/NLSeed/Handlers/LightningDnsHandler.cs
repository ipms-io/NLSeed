using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NLSeed.Handlers;

using DnsModels;
using Parsers;
using Services;

public class LightningDnsHandler(ILogger<LightningDnsHandler> logger, NetworkViewService networkViewService, IServiceProvider provider)
{
    public byte[] HandleLightningDns(DnsMessage r)
    {
        if (r.Questions.Count < 1)
        {
            throw new NullReferenceException("empty request");
        }
        var req = DnsRequestParser.ParseRequest(r.Questions[0].QName, r.Questions[0].QType);
        
        logger.LogTrace("Incoming request: subdomain={subdomain}, type={dnsTypeToString}", req.Subdomain, DnsTypeToString(req.QType));

        // Create a reply message.
        var m = new DnsMessage();
        m.SetReply(r);

        // Case 1: SOA request.
        if (req.Subdomain.StartsWith("soa"))
        {
            logger.LogTrace("Handling SOA request");
            throw new NotImplementedException();
            // DnsARecord soaResp = new DnsARecord
            // {
            //     Header = new DnsRRHeader
            //     {
            //         Name = r.Questions[0].QName,
            //         Type = 1,      // A record
            //         Class = 1,     // IN
            //         Ttl = 60
            //     },
            //     A = AuthoritativeIP
            // };
            // m.Answers.Add(soaResp);
        }
        // Case 2: Wildcard query (no node_id).
        else if (string.IsNullOrEmpty(req.NodeId))
        {
            switch (req.QType)
            {
                case 28: // AAAA
                    // HandleAAAAQuery(r, m, req.Subdomain);
                    break;
                case 1:  // A
                    var aHandler = provider.GetService<AQueryHandler>()
                                   ?? throw new Exception("Critical Exception");
                        aHandler.HandleQuery(r, m, req.Subdomain);
                    break;
                case 33: // SRV
                    var srvHandler = provider.GetService<SrvQueryHandler>()
                                     ?? throw new Exception("Critical Exception");
                    srvHandler.HandleQuery(r, m, req.Subdomain);
                    break;
            }
        }
        // Case 3: Specific node query.
        else
        {
            if (!networkViewService.ReachableNodes.TryGetValue(req.NodeId.ToLowerInvariant(), out var n))
            {
                logger.LogWarning("Unable to find node with ID {nodeId}", req.NodeId);
            }
            else
            {
                switch (req.QType)
                {
                    case 28:
                        // AddAAAAResponse(n, r.Questions[0].QName, m.Answers);
                        throw new NotImplementedException();
                    case 1:
                        AQueryHandler.AddAResponse(r, m, n);
                        break;
                }
            }
        }

        // Send the reply.
        var response = m.ToByteArray();
        logger.LogTrace("Replying with {answersCount} answers and {extrasCount} extras (len={length})", m.Answers.Count, m.Extras.Count, response.Length);
        return response;
    }
    
    private static string DnsTypeToString(ushort qtype)
    {
        return qtype switch
        {
            1 => "A",
            28 => "AAAA",
            33 => "SRV",
            _ => $"Type{qtype}",
        };
    }
}