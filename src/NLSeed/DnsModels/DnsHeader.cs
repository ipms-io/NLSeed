namespace NLSeed.DnsModels;

public class DnsHeader
{
    public ushort Id { get; set; }
    public ushort Flags { get; set; }
    public ushort QDCount { get; set; }
    public ushort ANCount { get; set; }
    public ushort NSCount { get; set; }
    public ushort ARCount { get; set; }

    public static DnsHeader Parse(byte[] data, ref int offset)
    {
        DnsHeader header = new DnsHeader();
        header.Id = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;
        header.Flags = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;
        header.QDCount = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;
        header.ANCount = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;
        header.NSCount = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;
        header.ARCount = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;
        return header;
    }

    public byte[] ToByteArray()
    {
        byte[] bytes = new byte[12];
        int offset = 0;
        bytes[offset++] = (byte)(Id >> 8);
        bytes[offset++] = (byte)(Id & 0xFF);
        bytes[offset++] = (byte)(Flags >> 8);
        bytes[offset++] = (byte)(Flags & 0xFF);
        bytes[offset++] = (byte)(QDCount >> 8);
        bytes[offset++] = (byte)(QDCount & 0xFF);
        bytes[offset++] = (byte)(ANCount >> 8);
        bytes[offset++] = (byte)(ANCount & 0xFF);
        bytes[offset++] = (byte)(NSCount >> 8);
        bytes[offset++] = (byte)(NSCount & 0xFF);
        bytes[offset++] = (byte)(ARCount >> 8);
        bytes[offset++] = (byte)(ARCount & 0xFF);
        return bytes;
    }
}