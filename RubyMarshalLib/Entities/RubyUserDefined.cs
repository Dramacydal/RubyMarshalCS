using System.Diagnostics;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyUserDefined : AbstractEntity
{
    public AbstractEntity ClassName { get; set; }

    public byte[] Bytes { get; set; }

    public override RubyCodes Code { get; protected set; } = RubyCodes.UserDefined;

    public override void ReadData(BinaryReader reader)
    {
        ClassName = Context.Read(reader);
        if (ClassName.Code != RubyCodes.SymbolLink)
        {
            Debug.WriteIf(false,"");
        }

        Bytes = reader.ReadByteSequence();
    }

    public override void WriteData(BinaryWriter writer)
    {
        Context.Write(writer, ClassName);
        writer.WriteByteSequence(Bytes);
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
