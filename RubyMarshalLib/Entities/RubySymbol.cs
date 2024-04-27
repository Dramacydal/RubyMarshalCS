using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubySymbol : AbstractEntity
{
    public string Name { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.Symbol;

    public override void ReadData(BinaryReader reader)
    {
        Name = Encoding.UTF8.GetString(reader.ReadByteSequence());
    }

    public override void WriteData(BinaryWriter writer)
    {
        var bytes = Encoding.UTF8.GetBytes(Name);
        
        writer.WriteByteSequence(bytes);
    }

    public override string ToString()
    {
        return Name;
    }
}