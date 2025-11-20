using System.IO.Compression;
using ZLibDotNet;

namespace RubyMarshalCS.Serialization;

public static class RubyDeflate
{
    private static ZLib _zlib = new();
    
    public static byte[] Deflate(byte[] data, int level = ZLib.Z_DEFAULT_COMPRESSION)
    {
        var bound = _zlib.CompressBound((uint)data.Length);

        var compressed = new byte[bound];

        var compressResult = _zlib.Compress(compressed, out var compressedLen, data, data.Length, level);
        if (compressResult != ZLib.Z_OK)
            throw new Exception($"Failed to deflate: {compressResult}");

        return compressed.Take(compressedLen).ToArray();
    }

    public static byte[] Inflate(byte[] data)
    {
        using MemoryStream compressedStream = new(data);
        using MemoryStream uncompressedStream = new();
        using (ZLibStream inflateStream = new(compressedStream, CompressionMode.Decompress))
            inflateStream.CopyTo(uncompressedStream);

        return uncompressedStream.ToArray();
    }
}
