using System.Text;
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubySymbol : AbstractEntity
{
    public string Name { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.Symbol;

    public override void ReadData(BinaryReader r)
    {
        var len = r.ReadPackedInt();

        Name = Encoding.UTF8.GetString(r.ReadBytes(len));
    }

    public override void WriteData(BinaryWriter w)
    {
        var bytes = Encoding.UTF8.GetBytes(Name);
        
        w.WritePackedInt(bytes.Length);
        w.Write(bytes);
    }
}