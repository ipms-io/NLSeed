using System.Text;

namespace NLSeed.DnsModels;

public class DnsSRVRecord: DnsRecord
{
    public ushort Priority { get; set; }
    public ushort Weight { get; set; }
    public ushort Port { get; set; }
    public string Target { get; set; }

    public override byte[] ToByteArray()
    {
        var bytes = new List<byte>();
        // Priority
        bytes.Add((byte)(Priority >> 8));
        bytes.Add((byte)(Priority & 0xFF));
        // Weight
        bytes.Add((byte)(Weight >> 8));
        bytes.Add((byte)(Weight & 0xFF));
        // Port
        bytes.Add((byte)(Port >> 8));
        bytes.Add((byte)(Port & 0xFF));
        // Target in DNS format
        bytes.AddRange(EncodeDomainName(Target));
        return bytes.ToArray();
    }

    private static byte[] EncodeDomainName(string domain)
    {
        var bytes = new List<byte>();
        var labels = domain.TrimEnd('.').Split('.');
        foreach (var label in labels)
        {
            var len = (byte)label.Length;
            bytes.Add(len);
            bytes.AddRange(Encoding.ASCII.GetBytes(label));
        }
        bytes.Add(0);
        return bytes.ToArray();
    }
}