using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyUserMarshal : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.UserMarshal;
    
    public AbstractEntity ClassName { get; set; }

    public AbstractEntity Object { get; set; }

    public string GetRealClassName()
    {
        return ClassName.ResolveIfLink().ToString()!;
    }

    public override string ToString()
    {
        return $"User marshal object: {GetRealClassName()}";
    }
}
