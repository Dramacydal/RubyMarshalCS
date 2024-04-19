using RubyMarshal.Enums;

namespace RubyMarshal.Entities;

public class RubyArray : AbstractEntity
{
    public List<AbstractEntity> Elements = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Array;

    public override void ReadData(BinaryReader r)
    {
        var size = r.ReadFixNum();

        for (var i = 0; i < size; ++i)
            Elements.Add(Context.Read(r));
    }

    public override void WriteData(BinaryWriter w)
    {
        w.WriteFixNum(Elements.Count);
        
        foreach (var e in Elements)
            Context.Write(w, e);
    }
}