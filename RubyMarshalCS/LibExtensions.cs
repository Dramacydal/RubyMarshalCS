using RubyMarshalCS.Entities;

namespace RubyMarshalCS;

public static class LibExtensions
{
    public static int ReadFixNum(this BinaryReader r)
    {
        var e = new RubyFixNum();
        e.ReadData(r);
        return e.Value;
    }
    
    public static void WriteFixNum(this BinaryWriter r, int value)
    {
        var e = new RubyFixNum();
        e.Value = value;
        e.WriteData(r);
    }

    public static byte[] ReadByteSequence(this BinaryReader reader)
    {
        var len = reader.ReadFixNum();
        return reader.ReadBytes(len);
    }

    public static void WriteByteSequence(this BinaryWriter writer, byte[] value)
    {
        writer.WriteFixNum(value.Length);
        writer.Write(value);
    }
}