using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyNil : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.Nil;

    public override void ReadData(BinaryReader reader)
    {
    }

    public override void WriteData(BinaryWriter writer)
    {
    }
}