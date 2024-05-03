using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyUserMarshal : AbstractEntity
{
    public AbstractEntity ClassName { get; set; }

    public AbstractEntity Object { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.UserMarshal;

    public override void ReadData(BinaryReader reader)
    {
        ClassName = Context.Read(reader);
        // Debug.Assert(ClassName.Code == RubyCodes.SymbolLink || ClassName.Code == RubyCodes.Symbol);
        Object = Context.Read(reader, true);
    }

    public override void WriteData(BinaryWriter writer)
    {
        Context.Write(writer, ClassName);
        Context.Write(writer, Object, true);
    }

    public string GetRealClassName()
    {
        return ClassName.ResolveIfLink().ToString()!;
    }

    public override string ToString()
    {
        return $"User object: {GetRealClassName()}";
    }
}
