using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyTrue : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.True;

    public override void ReadData(BinaryReader reader)
    {
    }

    public override void WriteData(BinaryWriter writer)
    {
    }
}
