using System.Text;

namespace NLSeed.DnsModels;

public class DnsQuestion
{
    public string QName { get; set; }
    public ushort QType { get; set; }
    public ushort QClass { get; set; }

    public static DnsQuestion Parse(byte[] data, ref int offset)
    {
        var question = new DnsQuestion
        {
            QName = ParseDomainName(data, ref offset),
            QType = (ushort)((data[offset] << 8) | data[offset + 1])
        };
        offset += 2;
        question.QClass = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;
        return question;
    }

    public byte[] ToByteArray()
    {
        var bytes = new List<byte>();
        bytes.AddRange(EncodeDomainName(QName));
        bytes.Add((byte)(QType >> 8));
        bytes.Add((byte)(QType & 0xFF));
        bytes.Add((byte)(QClass >> 8));
        bytes.Add((byte)(QClass & 0xFF));
        return bytes.ToArray();
    }

    // Encode a domain name into DNS label format.
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
        bytes.Add(0); // Terminating zero-length label.
        return bytes.ToArray();
    }

    // Parse a domain name from the DNS message.
    private static string ParseDomainName(byte[] data, ref int offset)
    {
        var sb = new StringBuilder();
        var originalOffset = offset;
        var jumped = false;
        var jumpOffset = 0;
        while (true)
        {
            if (offset >= data.Length)
                break;
            var len = data[offset++];
            if (len == 0)
                break;
            // Check for pointer (compression)
            if ((len & 0xC0) == 0xC0)
            {
                if (!jumped)
                {
                    jumpOffset = offset + 1;
                    jumped = true;
                }
                var pointer = ((len & 0x3F) << 8) | data[offset++];
                var savedOffset = offset;
                offset = pointer;
                sb.Append(ParseDomainName(data, ref offset));
                offset = savedOffset;
                break;
            }
            else
            {
                sb.Append(Encoding.ASCII.GetString(data, offset, len));
                offset += len;
                sb.Append('.');
            }
        }
        if (jumped)
            offset = jumpOffset;
        return sb.ToString();
    }
}