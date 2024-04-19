using System.Text;
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyString : AbstractEntity
{
    public string Value { get; set; }
    
    public byte[] Bytes { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.String;

    public override void ReadData(BinaryReader r)
    {
        var len = r.ReadFixNum();
        Bytes = r.ReadBytes(len);
        Value = Encoding.UTF8.GetString(Bytes);
    }

    public override void WriteData(BinaryWriter w)
    {
        w.WriteFixNum(Bytes.Length);
        w.Write(Bytes);
    }
}