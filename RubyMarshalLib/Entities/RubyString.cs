using System.Text;
using RubyMarshal.Enums;

namespace RubyMarshal.Entities;

public class RubyString : AbstractEntity
{
    public byte[] Bytes { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.String;

    public override void ReadData(BinaryReader r)
    {
        var len = r.ReadFixNum();
        Bytes = r.ReadBytes(len);
    }

    public override void WriteData(BinaryWriter w)
    {
        w.WriteFixNum(Bytes.Length);
        w.Write(Bytes);
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(Bytes);
    }
}