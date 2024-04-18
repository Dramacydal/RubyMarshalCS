using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyHash : AbstractEntity
{
    private List<KeyValuePair<AbstractEntity, AbstractEntity>> Pairs = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.RubyHash;

    public override void ReadData(BinaryReader r)
    {
        var numPairs = r.ReadPackedInt();

        for (var i = 0; i < numPairs; ++i)
        {
            var k = Factory.Read(r);
            var v = Factory.Read(r);

            Pairs.Add(new(k, v));
        }
    }

    public override void WriteData(BinaryWriter w)
    {
        w.WritePackedInt(Pairs.Count);

        foreach (var pair in Pairs)
        {
            Factory.Write(w, pair.Key);
            Factory.Write(w, pair.Value);
        }
    }
}