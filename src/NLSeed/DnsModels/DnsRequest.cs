namespace NLSeed.DnsModels;

public class DnsRequest
{
    public string Subdomain { get; set; }
    public ushort QType { get; set; }
    // “atypes” is defaulted to 6 unless overridden by the query
    public int ATypes { get; set; } = 6;
    public int Realm { get; set; }
    public string NodeId { get; set; }
}