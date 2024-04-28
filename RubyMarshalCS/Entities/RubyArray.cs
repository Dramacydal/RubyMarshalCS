using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyArray : AbstractEntity
{
    public List<AbstractEntity> Elements = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Array;

    public override void ReadData(BinaryReader reader)
    {
        var size = reader.ReadFixNum();

        for (var i = 0; i < size; ++i)
            Elements.Add(Context.Read(reader));
    }

    public override void WriteData(BinaryWriter writer)
    {
        writer.WriteFixNum(Elements.Count);
        
        foreach (var e in Elements)
            Context.Write(writer, e);
    }
}