using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyInstanceVariable : AbstractEntity
{
    public AbstractEntity Object { get; set; }

    public List<KeyValuePair<AbstractEntity,AbstractEntity>> Variables { get; } = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.InstanceVar;

    public override void ReadData(BinaryReader reader)
    {
        // never a link
        Object = Context.Read(reader, true);
        
        var numVars = reader.ReadFixNum();
        for (var i = 0; i < numVars; ++i)
        {
            var k = Context.Read(reader);
            var v = Context.Read(reader);

            Variables.Add(new(k, v));
        }
    }

    public override void WriteData(BinaryWriter writer)
    {
        Context.Write(writer, Object, true);
        writer.WriteFixNum(Variables.Count);

        foreach (var v in Variables)
        {
            Context.Write(writer, v.Key);
            Context.Write(writer, v.Value);
        }
    }
}