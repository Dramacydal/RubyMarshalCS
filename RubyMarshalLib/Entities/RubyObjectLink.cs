using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyObjectLink : AbstractEntity
{
    public int ReferenceId { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.ObjectLink;

    public override void ReadData(BinaryReader reader)
    {
        ReferenceId = reader.ReadFixNum();
    }

    public override void WriteData(BinaryWriter writer)
    {
        writer.WriteFixNum(ReferenceId);
    }
}
