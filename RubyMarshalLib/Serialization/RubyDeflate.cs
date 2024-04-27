using System.IO.Compression;

namespace RubyMarshalCS.Serialization;

public static class RubyDeflate
{
    public static byte[] Inflate(byte[] data)
    {
        if (data.Length < 6)
            return Array.Empty<byte>();

        using var deflated = new MemoryStream(data.Skip(2).Take(data.Length - 6).ToArray());
        using var inflated = new MemoryStream();

        using var decompressor = new DeflateStream(deflated, CompressionMode.Decompress);

        decompressor.CopyTo(inflated);

        inflated.Seek(0, SeekOrigin.Begin);

        var buffer = inflated.GetBuffer();

        if (inflated.Length < buffer.Length)
            Array.Resize(ref buffer, (int)inflated.Length);

        return buffer;
    }

    public static byte[] Deflate(byte[] data, CompressionLevel level = CompressionLevel.Fastest)
    {
        using var inflated = new MemoryStream();

        using (var compressor = new DeflateStream(inflated, level, true))
        {
            compressor.Write(data);            
        }

        var compressedData = new byte[inflated.Length];
        inflated.Seek(0, SeekOrigin.Begin);
        inflated.Read(compressedData, 0, compressedData.Length);

        List<byte> output = new(compressedData.Length + 2 + 4)
        {
            120, 156
        };

        output.AddRange(compressedData);

        var adler32 = BitConverter.GetBytes(Adler32(data));
        
        output.AddRange(BitConverter.IsLittleEndian ? adler32.Reverse() : adler32);
        
        return output.ToArray();
    }

    public static uint Adler32(byte[] data)
    {
        uint a = 1, b = 0;

        const uint modAdler = 65521;

        foreach (var t in data)
        {
            a = (a + t) % modAdler;
            b = (b + a) % modAdler;
        }

        return (b << 16) | a;
    }
}
