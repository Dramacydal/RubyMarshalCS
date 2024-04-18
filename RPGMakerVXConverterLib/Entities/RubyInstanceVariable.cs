using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyInstanceVariable : AbstractEntity
{
    private AbstractEntity Object { get; set; }

    private List<KeyValuePair<AbstractEntity,AbstractEntity>> Variables { get; set; } = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.InstanceVar;

    public override void ReadData(RubyFile r)
    {
        Object = r.Read(); // RubySymbol
        var numVars = r.ReadPackedInt();

        for (var i = 0; i < numVars; ++i)
        {
            var k = r.Read();
            var v = r.Read();

            Variables.Add(new(k, v));
        }
    }

    public override void WriteData(RubyFile f)
    {
        f.Write(Object);
        f.WritePackedInt(Variables.Count);

        foreach (var v in Variables)
        {
            f.Write(v.Key);
            f.Write(v.Value);
        }
    }
}