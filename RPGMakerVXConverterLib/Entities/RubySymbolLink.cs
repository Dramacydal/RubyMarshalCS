using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubySymbolLink : AbstractEntity
{
    public int ReferenceId { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.SymbolLink;

    public override void ReadData(BinaryReader r)
    {
        ReferenceId = r.ReadFixNum();
    }

    public override void WriteData(BinaryWriter w)
    {
        w.WriteFixNum(ReferenceId);
    }
}
