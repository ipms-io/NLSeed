using NLSeed.DnsModels;

namespace NLSeed.Interfaces;

public interface IQueryHandler
{
    void HandleQuery(DnsMessage request, DnsMessage response, string subDomain);
}