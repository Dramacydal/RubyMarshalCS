using System.Text;
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyString : AbstractEntity
{
    public string Value { get; set; }
    
    public byte[] Bytes { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.String;

    public override void ReadData(RubyFile r)
    {
        var len = r.ReadPackedInt();
        Bytes = r.Reader.ReadBytes(len);
        Value = Encoding.UTF8.GetString(Bytes);
    }

    public override void WriteData(RubyFile f)
    {
        f.WritePackedInt(Bytes.Length);
        f.Writer.Write(Bytes);
    }
}