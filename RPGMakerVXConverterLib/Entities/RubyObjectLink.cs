using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyObjectLink : AbstractEntity
{
    public int ReferenceId { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.ObjectLink;

    public override void ReadData(BinaryReader r)
    {
        ReferenceId = r.ReadPackedInt();
    }

    public override void WriteData(BinaryWriter w)
    {
        w.WritePackedInt(ReferenceId);
    }
}
