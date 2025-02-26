// using NLSeed.DnsModels;
// using NLSeed.Parsers;
//
// namespace NLSeed.Handlers;
//
// public static class LightningDnsHandler
// {
//     public static void HandleLightningDns(IDnsResponseWriter w, DnsMessage r)
//     {
//         if (r.Questions.Count < 1)
//         {
//             Console.Error.WriteLine("empty request");
//             return;
//         }
//         DnsRequest req;
//         try
//         {
//             req = DnsRequestParser.ParseRequest(r.Questions[0].QName, r.Questions[0].QType);
//         }
//         catch (Exception ex)
//         {
//             Console.Error.WriteLine($"error parsing request: {ex.Message}");
//             return;
//         }
//         Console.WriteLine($"Incoming request: subdomain={req.Subdomain}, type={DnsTypeToString(req.QType)}");
//
//         // Create a reply message.
//         var m = new DnsMessage();
//         m.SetReply(r);
//
//         // Case 1: SOA request.
//         if (req.Subdomain.StartsWith("soa"))
//         {
//             Console.WriteLine("Handling SOA request");
//             DnsARecord soaResp = new DnsARecord
//             {
//                 Header = new DnsRRHeader
//                 {
//                     Name = r.Questions[0].QName,
//                     Type = 1,      // A record
//                     Class = 1,     // IN
//                     Ttl = 60
//                 },
//                 A = AuthoritativeIP
//             };
//             m.Answers.Add(soaResp);
//         }
//         // Case 2: Wildcard query (no node_id).
//         else if (string.IsNullOrEmpty(req.NodeId))
//         {
//             switch (req.QType)
//             {
//                 case 28: // AAAA
//                     HandleAAAAQuery(r, m, req.Subdomain);
//                     break;
//                 case 1:  // A
//                     HandleAQuery(r, m, req.Subdomain);
//                     break;
//                 case 33: // SRV
//                     HandleSRVQuery(r, m, req.Subdomain);
//                     break;
//             }
//         }
//         // Case 3: Specific node query.
//         else
//         {
//             ChainView chainView = LocateChainView(req.Subdomain);
//             if (chainView == null)
//             {
//                 Console.Error.WriteLine($"node query: no chain view found for {req.Subdomain}");
//             }
//             else
//             {
//                 if (!chainView.NetView.ReachableNodes.TryGetValue(req.NodeId, out Node n))
//                 {
//                     Console.WriteLine($"Unable to find node with ID {req.NodeId}");
//                 }
//                 else
//                 {
//                     if (req.QType == 28)
//                     {
//                         AddAAAAResponse(n, r.Questions[0].QName, m.Answers);
//                     }
//                     else if (req.QType == 1)
//                     {
//                         AddAResponse(n, r.Questions[0].QName, m.Answers);
//                     }
//                 }
//             }
//         }
//
//         // Send the reply.
//         w.WriteMsg(m);
//         Console.WriteLine($"Replying with {m.Answers.Count} answers and {m.Extras.Count} extras (len={m.Len()})");
//     }
//     
//     private static string DnsTypeToString(ushort qtype)
//     {
//         return qtype switch
//         {
//             1 => "A",
//             28 => "AAAA",
//             33 => "SRV",
//             _ => $"Type{qtype}",
//         };
//     }
// }