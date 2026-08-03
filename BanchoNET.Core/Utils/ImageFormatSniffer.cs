using System.Buffers.Binary;

namespace BanchoNET.Core.Utils;

public readonly record struct SniffedImage(
    string Extension,
    int Width,
    int Height
);

public static class ImageFormatSniffer
{
    public static bool TrySniff(
        ReadOnlySpan<byte> header,
        out SniffedImage image
    ) {
        image = default;

        if (TryPng(header, out image)) return true;
        if (TryGif(header, out image)) return true;
        if (TryWebp(header, out image)) return true;
        if (TryJpeg(header, out image)) return true;

        return false;
    }

    private static bool TryPng(
        ReadOnlySpan<byte> header,
        out SniffedImage image
    ) {
        image = default;

        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (header.Length < 24 || !header[..8].SequenceEqual(signature)) return false;
        
        if (!header.Slice(12, 4).SequenceEqual("IHDR"u8)) return false;

        var width = (int)BinaryPrimitives.ReadUInt32BigEndian(header.Slice(16, 4));
        var height = (int)BinaryPrimitives.ReadUInt32BigEndian(header.Slice(20, 4));

        image = new SniffedImage("png", width, height);
        return true;
    }

    private static bool TryGif(
        ReadOnlySpan<byte> header,
        out SniffedImage image
    ) {
        image = default;

        if (header.Length < 10) return false;
        if (!header[..6].SequenceEqual("GIF87a"u8) && !header[..6].SequenceEqual("GIF89a"u8)) return false;

        var width = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(6, 2));
        var height = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(8, 2));

        image = new SniffedImage("gif", width, height);
        return true;
    }

    private static bool TryWebp(
        ReadOnlySpan<byte> header,
        out SniffedImage image
    ) {
        image = default;

        if (header.Length < 30) return false;
        if (!header[..4].SequenceEqual("RIFF"u8) || !header.Slice(8, 4).SequenceEqual("WEBP"u8)) return false;

        var chunk = header.Slice(12, 4);

        if (chunk.SequenceEqual("VP8 "u8))
        {
            var width = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(26, 2)) & 0x3FFF;
            var height = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(28, 2)) & 0x3FFF;

            image = new SniffedImage("webp", width, height);
            return true;
        }

        if (chunk.SequenceEqual("VP8L"u8))
        {
            var bits = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(21, 4));

            image = new SniffedImage("webp", (int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
            return true;
        }

        if (chunk.SequenceEqual("VP8X"u8))
        {
            var width = header[24] | (header[25] << 8) | (header[26] << 16);
            var height = header[27] | (header[28] << 8) | (header[29] << 16);

            image = new SniffedImage("webp", width + 1, height + 1);
            return true;
        }

        return false;
    }
    
    public static bool TryJpegFull(
        ReadOnlySpan<byte> data,
        out SniffedImage image
    ) {
        image = default;

        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8) return false;

        var offset = 2;

        while (offset + 9 < data.Length)
        {
            if (data[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            var marker = data[offset + 1];
            offset += 2;
            
            if (marker is 0xD8 or 0x01 || marker is >= 0xD0 and <= 0xD7) continue;
            if (marker == 0xD9) break;

            if (offset + 1 >= data.Length) break;

            var length = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            if (length < 2) break;
            
            if (marker is >= 0xC0 and <= 0xCF && marker is not (0xC4 or 0xC8 or 0xCC))
            {
                if (offset + 7 >= data.Length) break;

                var height = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 3, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 5, 2));

                image = new SniffedImage("jpg", width, height);
                return true;
            }

            offset += length;
        }

        return false;
    }

    private static bool TryJpeg(
        ReadOnlySpan<byte> header,
        out SniffedImage image
    ) => TryJpegFull(header, out image);
}