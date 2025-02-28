using System.Net;

namespace NLSeed.DnsModels;

public class DnsMessage
    {
        public DnsHeader Header { get; set; }
        public List<DnsQuestion> Questions { get; set; } = [];
        public List<DnsRecord> Answers { get; set; } = [];
        public List<DnsRecord> Extras { get; set; } = [];

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
            
            Header.ANCount = (ushort)Answers.Count;
            bytes.AddRange(Header.ToByteArray());
            
            foreach (var answer in Answers)
            {
                bytes.AddRange(Questions[0].EncodedQName());

                if (answer is DnsSRVRecord srv)
                {
                    // Type (SRV = 33)
                    bytes.Add(0x00);
                    bytes.Add(0x21);
                    // Class (INET = 1)
                    bytes.Add(0x00);
                    bytes.Add(0x01);
                    // TTL (4 bytes) – we use 60 seconds
                    bytes.Add(0);
                    bytes.Add(0);
                    bytes.Add(0);
                    bytes.Add(60 & 0xFF);
                    // RDLENGTH (2 bytes)
                    var rdata = srv.ToByteArray();
                    var rdLength = (ushort)rdata.Length;
                    bytes.Add((byte)(rdLength >> 8));
                    bytes.Add((byte)(rdLength & 0xFF));
                    // RDATA
                    bytes.AddRange(rdata);
                } 
                else if (answer is DnsARecord a)
                {
                    // TYPE (A = 1)
                    bytes.Add(0x00);
                    bytes.Add(0x01);
                    // CLASS (INET = 1)
                    bytes.Add(0x00);
                    bytes.Add(0x01);
                    // TTL (4 bytes) – 60 seconds in big-endian.
                    bytes.Add(0);
                    bytes.Add(0);
                    bytes.Add(0);
                    bytes.Add(60 & 0xFF);
                    // RDATA: For an A record, this is the 4-byte IPv4 address.
                    var rdata = a.ToByteArray(); // Expected to be exactly 4 bytes.
                    var rdLength = (ushort)rdata.Length;  // Should be 4.
                    // RDLENGTH (2 bytes) in big-endian.
                    bytes.Add((byte)(rdLength >> 8));
                    bytes.Add((byte)(rdLength & 0xFF));
                    // Append RDATA.
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
            Header = new DnsHeader
            {
                Id = query.Header.Id,
                Flags = (ushort)(query.Header.Flags | 0x8000)
            };
            Questions = query.Questions;
        }
    }