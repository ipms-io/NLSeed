using System.Net;
using MessagePack;
using NLSeed.Formatters;

namespace NLSeed.Models;

[MessagePackObject]
public class Node(string id)
{
    [Key(0)]
    public string Id { get; } = id;
    
    [Key(1)]
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    
    [Key(2)]
    public byte Type { get; set; }
    
    [Key(3)]
    [MessagePackFormatter(typeof(IpEndPointCollectionFormatter))]
    public ICollection<IPEndPoint> Addresses { get; set; } = new List<IPEndPoint>(2);

    public override string ToString()
    {
        return $"{Id}@{Addresses.FirstOrDefault()}";
    }

    public override bool Equals(object? obj)
    {
        return obj is Node other && Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}