using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyArray : AbstractEntity
{
    private List<AbstractEntity> Elements = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Array;

    public override void ReadData(RubyFile r)
    {
        var size = r.ReadPackedInt();

        for (var i = 0; i < size; ++i)
            Elements.Add(r.Read());
    }

    public override void WriteData(RubyFile f)
    {
        f.WritePackedInt(Elements.Count);
        
        foreach (var e in Elements)
            e.WriteData(f);
    }
}