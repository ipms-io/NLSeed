using System.Text;
using NBitcoin.DataEncoders;

namespace NLSeed.Encoders;

public class Bech32Encoder(string hrp = "ln") : NBitcoin.DataEncoders.Bech32Encoder(Encoding.UTF8.GetBytes(hrp))
{
    public string EncodeNodeId(string nodeId)
    {
        var rawNodeId = HexDecodeString(nodeId);
        var convertedId = ConvertBits(rawNodeId.AsReadOnly(), 8, 5);
        return EncodeRaw(convertedId, Bech32EncodingType.BECH32);
    }

    public string DecodeNodeId(string encodedNodeId)
    {
        var decodedBytes = DecodeDataRaw(encodedNodeId, out _);
        var convertedId = ConvertBits(decodedBytes.AsReadOnly(), 5, 8, false);
        return Convert.ToHexString(convertedId);
    }
    
    private static byte[] HexDecodeString(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        var numberChars = hex.Length;
        var bytes = new byte[numberChars / 2];
        for (var i = 0; i < numberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
}