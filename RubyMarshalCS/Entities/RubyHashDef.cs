using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyHashDef : AbstractEntity
{
    public List<Tuple<AbstractEntity, AbstractEntity>> Pairs = new();

    public AbstractEntity Default;
    
    public override RubyCodes Code { get; protected set; } = RubyCodes.HashDef;

    public override void ReadData(BinaryReader reader)
    {
        var numPairs = reader.ReadFixNum();

        for (var i = 0; i < numPairs; ++i)
        {
            var k = Context.Read(reader);
            var v = Context.Read(reader);

            Pairs.Add(new(k, v));
        }

        Default = Context.Read(reader);
    }

    public override void WriteData(BinaryWriter writer)
    {
        writer.WriteFixNum(Pairs.Count);

        foreach (var pair in Pairs)
        {
            Context.Write(writer, pair.Item1);
            Context.Write(writer, pair.Item2);
        }
    }
}