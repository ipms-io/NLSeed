using System.Globalization;

namespace NLSeed.Parsers;

using DnsModels;
using Encoders;

public static class DnsRequestParser
{
    public static string RootDomain = "ipms.io";
    
    public static DnsRequest ParseRequest(string name, ushort qtype)
    {
        // Convert name to lower case.
        name = name.ToLowerInvariant();

        // Check that the name ends with "<rootDomain>." (for example, "example.com.")
        if (!name.EndsWith($"{RootDomain.ToLowerInvariant()}."))
            throw new Exception($"malformed request: {name}");

        // Accept only query types A (1), AAAA (28) or SRV (33)
        if (qtype != 1 && qtype != 28 && qtype != 33)
            throw new Exception($"refusing to handle query type {qtype}");

        // Extract the subdomain part by removing the root domain and the trailing period.
        var subdomainLength = name.Length - RootDomain.Length - 1;
        var subdomain = name[..subdomainLength];

        var req = new DnsRequest
        {
            Subdomain = subdomain,
            QType = qtype,
            ATypes = 6
        };

        Console.WriteLine($"Dispatching request for sub-domain {req.Subdomain}");

        // If the subdomain starts with "soa", return a slimmed-down request.
        if (req.Subdomain.StartsWith("soa"))
        {
            return new DnsRequest { Subdomain = req.Subdomain };
        }

        // Split the subdomain into parts using '.' as the delimiter.
        var parts = req.Subdomain.Split('.', StringSplitOptions.RemoveEmptyEntries);

        foreach (var cond in parts)
        {
            // Skip empty parts and specific chain-related labels ("ltc" or "test").
            if (string.IsNullOrEmpty(cond) || cond == "ltc" || cond == "test")
                continue;

            var k = cond[0];
            var v = cond[1..];

            switch (k)
            {
                case 'r':
                {
                    // Parse the realm number.
                    if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int realm))
                        req.Realm = realm;
                    break;
                }
                // only for SRV queries
                case 'a' when qtype == 33:
                {
                    // Override ATypes from default if specified.
                    if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int atypes))
                        req.ATypes = atypes;
                    break;
                }
                case 'l':
                {
                    req.NodeId = new Bech32Encoder().DecodeNodeId(cond);
                    break;
                }
            }
        }

        return req;
    }
}