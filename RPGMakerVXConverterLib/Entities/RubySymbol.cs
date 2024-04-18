using System.Text;
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubySymbol : AbstractEntity
{
    public string Name { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.Symbol;

    public override void ReadData(RubyFile r)
    {
        var len = r.ReadPackedInt();

        Name = Encoding.UTF8.GetString(r.Reader.ReadBytes(len));
    }

    public override void WriteData(RubyFile f)
    {
        var bytes = Encoding.UTF8.GetBytes(Name);
        
        f.WritePackedInt(bytes.Length);
        f.Writer.Write(bytes);
    }
}