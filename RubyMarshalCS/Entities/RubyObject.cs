using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyObject : AbstractEntity
{
    public AbstractEntity ClassName { get; set; }
    
    public List<KeyValuePair<AbstractEntity, AbstractEntity>> Fields { get; } = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Object;

    public override void ReadData(BinaryReader reader)
    {
        ClassName = Context.Read(reader);

        var propCnt = reader.ReadFixNum();

        for (var i = 0; i < propCnt; ++i)
        {
            var k = Context.Read(reader);
            var v = Context.Read(reader);

            Fields.Add(new(k, v));
        }
    }

    public override void WriteData(BinaryWriter writer)
    {
        Context.Write(writer, ClassName);
        
        writer.WriteFixNum(Fields.Count);
        foreach (var field in Fields)
        {
            Context.Write(writer, field.Key);
            Context.Write(writer, field.Value);
        }
    }

    public string GetRealClassName()
    {
        return ClassName.ResolveIfLink().ToString();
    }

    public override string ToString()
    {
        return "Object: " + GetRealClassName();
    }
}