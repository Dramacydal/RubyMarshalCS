using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyObject : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.Object;
    
    public AbstractEntity ClassName { get; set; }
    
    public readonly List<KeyValuePair<AbstractEntity, AbstractEntity>> Fields = new();

    public string GetRealClassName()
    {
        return ClassName.ResolveIfLink().ToString();
    }

    public override string ToString()
    {
        return "Object: " + GetRealClassName();
    }
}