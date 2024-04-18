using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyInstanceVariable : AbstractEntity
{
    private AbstractEntity Object { get; set; }

    private List<KeyValuePair<AbstractEntity,AbstractEntity>> Variables { get; set; } = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.InstanceVar;

    public override void ReadData(BinaryReader r)
    {
        Object = Factory.Read(r); // RubySymbol
        var numVars = r.ReadPackedInt();

        for (var i = 0; i < numVars; ++i)
        {
            var k = Factory.Read(r);
            var v = Factory.Read(r);

            Variables.Add(new(k, v));
        }
    }

    public override void WriteData(BinaryWriter w)
    {
        Factory.Write(w, Object);
        w.WritePackedInt(Variables.Count);

        foreach (var v in Variables)
        {
            Factory.Write(w, v.Key);
            Factory.Write(w, v.Value);
        }
    }
}