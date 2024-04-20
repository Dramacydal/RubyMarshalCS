using RubyMarshal.Enums;

namespace RubyMarshal.Entities;

public class RubyHash : AbstractEntity
{
    public List<KeyValuePair<AbstractEntity, AbstractEntity>> Pairs = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Hash;

    public override void ReadData(BinaryReader r)
    {
        var numPairs = r.ReadFixNum();

        for (var i = 0; i < numPairs; ++i)
        {
            var k = Context.Read(r);
            var v = Context.Read(r);

            Pairs.Add(new(k, v));
        }
    }

    public override void WriteData(BinaryWriter w)
    {
        w.WriteFixNum(Pairs.Count);

        foreach (var pair in Pairs)
        {
            Context.Write(w, pair.Key);
            Context.Write(w, pair.Value);
        }
    }
}