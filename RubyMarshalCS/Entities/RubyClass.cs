using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyClass : AbstractEntity
{
    public byte[] Bytes { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.Class;

    public override void ReadData(BinaryReader reader)
    {
        Bytes = reader.ReadByteSequence();
    }

    public override void WriteData(BinaryWriter writer)
    {
        writer.WriteByteSequence(Bytes);
    }
}
