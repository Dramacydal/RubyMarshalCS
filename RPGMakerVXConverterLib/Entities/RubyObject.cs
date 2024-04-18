using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyObject : AbstractEntity
{
    public AbstractEntity ClassName { get; set; }
    
    public List<KeyValuePair<AbstractEntity, AbstractEntity>> Fields = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Object;

    public override void ReadData(BinaryReader r)
    {
        ClassName = Factory.Read(r);

        var propCnt = r.ReadPackedInt();

        for (var i = 0; i < propCnt; ++i)
        {
            var k = Factory.Read(r);
            var v = Factory.Read(r);

            Fields.Add(new(k, v));
        }
    }

    public override void WriteData(BinaryWriter w)
    {
        Factory.Write(w, ClassName);
        w.WritePackedInt(Fields.Count);

        foreach (var field in Fields)
        {
            Factory.Write(w, field.Key);
            Factory.Write(w, field.Value);
        }
    }
}