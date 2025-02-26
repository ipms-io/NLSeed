namespace NLSeed.DnsModels;

public abstract class DnsRecord
{
    public DnsRRHeader Header { get; set; }
    public abstract byte[] ToByteArray();
}