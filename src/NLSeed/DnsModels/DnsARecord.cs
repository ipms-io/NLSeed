using System.Net;
using System.Net.Sockets;

namespace NLSeed.DnsModels;

public class DnsARecord : DnsRecord
{
    /// <summary>
    /// The IPv4 address for this A record.
    /// </summary>
    public IPAddress A { get; set; }

    /// <summary>
    /// Converts this A record’s RDATA (the IP address) into a byte array.
    /// For an A record, the RDATA is exactly 4 bytes.
    /// </summary>
    /// <returns>Byte array containing the IPv4 address in network byte order.</returns>
    public override byte[] ToByteArray()
    {
        if (A == null)
            throw new InvalidOperationException("No IP address specified for A record.");

        // Ensure that the address is IPv4.
        if (A.AddressFamily != AddressFamily.InterNetwork)
            throw new InvalidOperationException("A record requires an IPv4 address.");

        return A.GetAddressBytes();
    }

    /// <summary>
    /// For debugging purposes, returns a string representation of this A record.
    /// </summary>
    public override string ToString()
    {
        return $"{Header?.Name ?? "<unknown>"} A {A}";
    }
}