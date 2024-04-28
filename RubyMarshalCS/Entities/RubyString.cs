using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyString : AbstractEntity
{
    public byte[] Bytes { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.String;

    public override void ReadData(BinaryReader reader)
    {
        Bytes = reader.ReadByteSequence();
    }

    public override void WriteData(BinaryWriter writer)
    {
        writer.WriteByteSequence(Bytes);
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(Bytes);
    }
}