using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubySymbolLink : AbstractEntity
{
    public int ReferenceId { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.SymbolLink;

    public override void ReadData(RubyFile r)
    {
        ReferenceId = r.ReadPackedInt();
    }

    public override void WriteData(RubyFile f)
    {
        f.WritePackedInt(ReferenceId);
    }
}
