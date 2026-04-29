using SevenZip;
using SevenZip.Compression.LZMA;

namespace BanchoNET.Core.Utils.Replays;

// ReSharper disable once InconsistentNaming
public static class LZMAHelper
{
    public static MemoryStream Compress(Stream inStream)
    {
        inStream.Position = 0;

        CoderPropID[] propIDs =
        {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
        };

        object[] properties =
        {
            (1 << 16),
            2,
            3,
            0,
            2,
        };

        var outStream = new MemoryStream();
        var encoder = new Encoder();
        
        encoder.SetCoderProperties(propIDs, properties);
        encoder.WriteCoderProperties(outStream);
        
        for (var i = 0; i < 8; i++)
            outStream.WriteByte((byte)(inStream.Length >> (8 * i)));
        
        encoder.Code(inStream, outStream, -1, -1, null);
        outStream.Flush();
        outStream.Position = 0;

        return outStream;
    }
    
    public static byte[] Compress(byte[] inBytes)
    {
        using (var ms = new MemoryStream(inBytes, false))
            return Compress(ms).ToArray();
    }
    
    public static MemoryStream Decompress(Stream inStream)
    {
        var decoder = new Decoder();

        var properties = new byte[5];
        if (inStream.Read(properties, 0, 5) != 5)
            throw new Exception("input .lzma is too short");
        decoder.SetDecoderProperties(properties);

        long outSize = 0;
        for (var i = 0; i < 8; i++)
        {
            var v = inStream.ReadByte();
            if (v < 0)
                break;
            outSize |= (long)(byte)v << (8 * i);
        }
        var compressedSize = inStream.Length - inStream.Position;

        var outStream = new MemoryStream();
        decoder.Code(inStream, outStream, compressedSize, outSize, null);
        outStream.Flush();
        outStream.Position = 0;
        return outStream;
    }
    
    public static byte[] Decompress(byte[] inBytes)
    {
        using (var ms = new MemoryStream(inBytes, false))
            return Decompress(ms).ToArray();
    }
}