using System.Net;
using MessagePack;
using MessagePack.Formatters;

namespace NLSeed.Formatters;

public class IpEndPointCollectionFormatter: IMessagePackFormatter<ICollection<IPEndPoint>>
{
    public void Serialize(ref MessagePackWriter writer, ICollection<IPEndPoint>? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }
        // Write array header with the number of elements
        writer.WriteArrayHeader(value.Count);
        var formatter = new IpEndPointFormatter();
        foreach (var endpoint in value)
        {
            formatter.Serialize(ref writer, endpoint, options);
        }
    }

    public ICollection<IPEndPoint>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil()) return null;
        var count = reader.ReadArrayHeader();
        var list = new List<IPEndPoint>(count);
        var formatter = new IpEndPointFormatter();
        for (var i = 0; i < count; i++)
        {
            list.Add(formatter.Deserialize(ref reader, options));
        }
        return list;
    }
}