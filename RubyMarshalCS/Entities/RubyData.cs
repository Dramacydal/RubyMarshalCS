using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyData : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.Data;
    
    public AbstractEntity ClassName { get; set; }
    
    public AbstractEntity Object { get; set; }

    public string GetRealClassName()
    {
        return ClassName.ResolveIfLink().ToString();
    }
    
    public override string ToString() => "Data: " + GetRealClassName();
}
