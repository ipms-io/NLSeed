using Microsoft.Extensions.Logging;
using NLSeed.Interfaces;

namespace NLSeed.Handlers;

using DnsModels;
using Encoders;
using Parsers;
using Services;

public class SrvQueryHandler(ILogger<LightningDnsHandler> logger, NetworkViewService networkViewService) : IQueryHandler
{
    public void HandleQuery(DnsMessage request, DnsMessage response, string subdomain)
    {
        logger.LogTrace("Handling SRV query target subdomain: {subdomain}", subdomain);

        subdomain = subdomain.Trim();
        // Split the subdomain on periods.
        var segments = subdomain.Split('.', StringSplitOptions.RemoveEmptyEntries);

        var prefix = segments.Length switch
        {
            4 or 3 => segments[2],
            2 => segments[0],
            _ => ""
        };
        
        // Build a DNS header for the SRV answers.
        var header = new DnsRRHeader
        {
            Name = request.Questions[0].QName,
            Type = 33,    // SRV
            Class = 1,    // IN
            Ttl = 60
        };

        // Get a random sample of nodes from the network view.
        // var nodes = networkViewService.RandomSample(255, 25);
        var nodes = networkViewService.ReachableNodes.Take(2).Select(x => x.Value).ToList();
        foreach (var n in nodes)
        {
            string encodedId;
            try
            {
                encodedId = new Bech32Encoder().EncodeNodeId(n.Id);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unable to encode key={id}", n.Id);
                continue;
            }

            // Construct the node name: "<encodedId>.<prefix><rootDomain>."
            var nodeName = $"{encodedId}.{prefix}{DnsRequestParser.RootDomain}.";

            foreach (var address in n.Addresses)
            {
                var rr = new DnsSRVRecord
                {
                    Header = header,
                    Priority = 10,
                    Weight = 10,
                    Port = (ushort)address.Port,
                    Target = nodeName
                };

                response.Answers.Add(rr);
            }
        }
    }
}