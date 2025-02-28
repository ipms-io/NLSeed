using Microsoft.Extensions.Logging;
using NLSeed.Models;

namespace NLSeed.Handlers;

using DnsModels;
using Interfaces;
using Services;

public class AQueryHandler(ILogger<LightningDnsHandler> logger, NetworkViewService networkViewService) : IQueryHandler
{   
    public void HandleQuery(DnsMessage request, DnsMessage response, string _)
    {
        logger.LogDebug("Handling A query");

        var nodes = networkViewService.RandomSample(2, 25);
        foreach (var n in nodes)
        {
            AddAResponse(request, response, n);
        }
    }

    public static void AddAResponse(DnsMessage request, DnsMessage response, Node node)
    {
        foreach (var address in node.Addresses)
        {
            response.Answers.Add(new DnsARecord
            {
                Header = new DnsRRHeader
                {
                    Name = request.Questions[0].QName,
                    Type = 1, // A record
                    Class = 1, // IN
                    Ttl = 60
                },
                A = address.Address
            });
        }
    }
}