using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyFixNum : AbstractEntity
{
    public int Value { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.FixNum;

    public override void ReadData(BinaryReader reader)
    {
        Value = reader.ReadFixNum();
    }

    public override void WriteData(BinaryWriter writer)
    {
        writer.WriteFixNum(Value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
