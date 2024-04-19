using RubyMarshal.Enums;

namespace RubyMarshal.Entities;

public class RubyObjectLink : AbstractEntity
{
    public int ReferenceId { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.ObjectLink;

    public override void ReadData(BinaryReader r)
    {
        ReferenceId = r.ReadFixNum();
    }

    public override void WriteData(BinaryWriter w)
    {
        w.WriteFixNum(ReferenceId);
    }
}
