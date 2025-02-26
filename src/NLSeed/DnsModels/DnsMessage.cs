using System.Net;

namespace NLSeed.DnsModels;

public class DnsMessage
    {
        public DnsHeader Header { get; set; }
        public List<DnsQuestion> Questions { get; set; } = new List<DnsQuestion>();
        public List<DnsRecord> Answers { get; set; } = new List<DnsRecord>();

        public static DnsMessage Parse(byte[] data)
        {
            var offset = 0;
            var message = new DnsMessage
            {
                Header = DnsHeader.Parse(data, ref offset)
            };
            for (var i = 0; i < message.Header.QDCount; i++)
            {
                message.Questions.Add(DnsQuestion.Parse(data, ref offset));
            }
            // We ignore Answer, Authority, Additional sections in the query.
            return message;
        }

        public byte[] ToByteArray()
        {
            var bytes = new List<byte>();
            // Update answer count.
            Header.ANCount = (ushort)Answers.Count;
            bytes.AddRange(Header.ToByteArray());
            foreach (var question in Questions)
            {
                bytes.AddRange(question.ToByteArray());
            }
            // For answers, we’ll write a very simple response.
            // For simplicity, we use a pointer for the Name field in the answer: 0xC00C points to offset 12.
            foreach (var answer in Answers)
            {
                bytes.Add(0xC0);
                bytes.Add(0x0C);
                if (answer is DnsSRVRecord srv)
                {
                    // Type (SRV = 33)
                    bytes.Add(0x00);
                    bytes.Add(0x21);
                    // Class (INET = 1)
                    bytes.Add(0x00);
                    bytes.Add(0x01);
                    // TTL (4 bytes) – we use 60 seconds
                    var ttl = 60;
                    bytes.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(ttl)));
                    // RDLENGTH (2 bytes)
                    var rdata = srv.ToByteArray();
                    var rdLength = (ushort)rdata.Length;
                    bytes.Add((byte)(rdLength >> 8));
                    bytes.Add((byte)(rdLength & 0xFF));
                    // RDATA
                    bytes.AddRange(rdata);
                }
            }
            return bytes.ToArray();
        }
        
        /// <summary>
        /// Sets this message as a reply to the given query.
        /// (For simplicity, we copy the Id and question.)
        /// </summary>
        public void SetReply(DnsMessage query)
        {
            Header.Id = query.Header.Id;
            // Set the reply flag (QR bit).
            Header.Flags = (ushort)(query.Header.Flags | 0x8000);
            Questions = query.Questions;
        }
    }