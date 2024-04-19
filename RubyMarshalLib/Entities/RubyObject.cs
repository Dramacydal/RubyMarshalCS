using RubyMarshal.Enums;

namespace RubyMarshal.Entities;

public class RubyObject : AbstractEntity
{
    public AbstractEntity ClassName { get; set; }
    
    public List<KeyValuePair<AbstractEntity, AbstractEntity>> Fields = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Object;

    public override void ReadData(BinaryReader r)
    {
        ClassName = Context.Read(r);

        var propCnt = r.ReadFixNum();

        for (var i = 0; i < propCnt; ++i)
        {
            var k = Context.Read(r);
            var v = Context.Read(r);

            Fields.Add(new(k, v));
        }
    }

    public override void WriteData(BinaryWriter w)
    {
        Context.Write(w, ClassName);
        w.WriteFixNum(Fields.Count);

        foreach (var field in Fields)
        {
            Context.Write(w, field.Key);
            Context.Write(w, field.Value);
        }
    }

    public override string ToString()
    {
        return "Object: " + ClassName.ToString();
    }
}