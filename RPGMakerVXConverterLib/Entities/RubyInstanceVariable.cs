using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyInstanceVariable : AbstractEntity
{
    private AbstractEntity Object { get; set; }

    private List<KeyValuePair<AbstractEntity,AbstractEntity>> Variables { get; set; } = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.InstanceVar;

    public override void ReadData(BinaryReader r)
    {
        Object = Context.Read(r); // RubySymbol
        var numVars = r.ReadFixNum();

        for (var i = 0; i < numVars; ++i)
        {
            var k = Context.Read(r);
            var v = Context.Read(r);

            Variables.Add(new(k, v));
        }
    }

    public override void WriteData(BinaryWriter w)
    {
        Context.Write(w, Object);
        w.WriteFixNum(Variables.Count);

        foreach (var v in Variables)
        {
            Context.Write(w, v.Key);
            Context.Write(w, v.Value);
        }
    }
}