using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyObject : AbstractEntity
{
    public AbstractEntity ClassName { get; set; }
    
    public List<KeyValuePair<AbstractEntity, AbstractEntity>> Fields = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Object;

    public override void ReadData(RubyFile r)
    {
        var s = r.Read();
        ClassName = s;

        var propCnt = r.ReadPackedInt();

        for (var i = 0; i < propCnt; ++i)
        {
            var k = r.Read();
            var v = r.Read();

            Fields.Add(new(k, v));
        }
    }

    public override void WriteData(RubyFile f)
    {
        f.Write(ClassName);
        f.WritePackedInt(Fields.Count);

        foreach (var field in Fields)
        {
            f.Write(field.Key);
            f.Write(field.Value);
        }
    }
}