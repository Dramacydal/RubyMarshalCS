using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyUserDefined : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.UserDefined;
    
    public AbstractEntity ClassName { get; set; }

    public byte[] Bytes { get; set; }

    public string GetRealClassName()
    {
        return ClassName.ResolveIfLink().ToString()!;
    }

    public override string ToString()
    {
        return $"User defined object: {GetRealClassName()}";
    }
}
