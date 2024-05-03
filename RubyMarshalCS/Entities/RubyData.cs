using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyData : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.Data;
    
    public AbstractEntity ClassName { get; set; }
    
    public AbstractEntity Object { get; set; }

    public string GetRealClassName()
    {
        return ClassName.ResolveIfLink().ToString();
    }
    
    public override string ToString() => "Data: " + GetRealClassName();
}
