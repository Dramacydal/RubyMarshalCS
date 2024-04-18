using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyHash : AbstractEntity
{
    private List<KeyValuePair<AbstractEntity, AbstractEntity>> Pairs = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.RubyHash;

    public override void ReadData(RubyFile f)
    {
        var numPairs = f.ReadPackedInt();

        for (var i = 0; i < numPairs; ++i)
        {
            var k = f.Read();
            var v = f.Read();

            Pairs.Add(new(k, v));
        }
    }

    public override void WriteData(RubyFile f)
    {
        f.WritePackedInt(Pairs.Count);

        foreach (var pair in Pairs)
        {
            f.Write(pair.Key);
            f.Write(pair.Value);
        }
    }
}