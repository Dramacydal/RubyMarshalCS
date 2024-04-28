using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyFalse : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.False;

    public override void ReadData(BinaryReader reader)
    {
    }

    public override void WriteData(BinaryWriter writer)
    {
    }
}
