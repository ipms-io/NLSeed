using System.Net;
using System.Text;

namespace NLSeed.DnsModels;

public class DnsRRHeader
{
    public string Name { get; set; }
    public ushort Type { get; set; }
    public ushort Class { get; set; }
    public int Ttl { get; set; }

    public byte[] ToByteArray()
    {
        var bytes = new List<byte>();
        bytes.AddRange(EncodeDomainName(Name));
        bytes.Add((byte)(Type >> 8));
        bytes.Add((byte)(Type & 0xFF));
        bytes.Add((byte)(Class >> 8));
        bytes.Add((byte)(Class & 0xFF));
        bytes.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Ttl)));
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