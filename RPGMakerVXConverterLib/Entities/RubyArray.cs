using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyArray : AbstractEntity
{
    private List<AbstractEntity> Elements = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Array;

    public override void ReadData(BinaryReader r)
    {
        var size = r.ReadPackedInt();

        for (var i = 0; i < size; ++i)
            Elements.Add(Factory.Read(r));
    }

    public override void WriteData(BinaryWriter w)
    {
        w.WritePackedInt(Elements.Count);
        
        foreach (var e in Elements)
            Factory.Write(w, e);
    }
}