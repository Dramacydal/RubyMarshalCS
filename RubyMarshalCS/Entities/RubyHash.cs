using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyHash : AbstractEntity
{
    public List<KeyValuePair<AbstractEntity, AbstractEntity>> Pairs = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Hash;

    public override void ReadData(BinaryReader reader)
    {
        var numPairs = reader.ReadFixNum();

        for (var i = 0; i < numPairs; ++i)
        {
            var k = Context.Read(reader);
            var v = Context.Read(reader);

            Pairs.Add(new(k, v));
        }
    }

    public override void WriteData(BinaryWriter writer)
    {
        writer.WriteFixNum(Pairs.Count);

        foreach (var pair in Pairs)
        {
            Context.Write(writer, pair.Key);
            Context.Write(writer, pair.Value);
        }
    }
}