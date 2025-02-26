using System.Net;
using MessagePack;
using MessagePack.Formatters;

namespace NLSeed.Formatters;

public class IpEndPointFormatter: IMessagePackFormatter<IPEndPoint?>
{
    public void Serialize(ref MessagePackWriter writer, IPEndPoint? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }
        
        // Serialize as an array with 2 elements: IP address string and port.
        writer.WriteArrayHeader(2);
        writer.Write(value.Address.ToString());
        writer.Write(value.Port);
    }

    public IPEndPoint? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil()) return null;
        
        var count = reader.ReadArrayHeader();
        if (count != 2)
        {
            throw new InvalidOperationException("Invalid IPEndPoint format.");
        }
        
        var addressStr = reader.ReadString();
        var port = reader.ReadInt32();
        return new IPEndPoint(IPAddress.Parse(addressStr!), port);
    }
}