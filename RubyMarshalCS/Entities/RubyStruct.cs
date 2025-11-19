using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyStruct : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.Struct;

    public AbstractEntity Name { get; set; }
    
    public List<KeyValuePair<AbstractEntity, AbstractEntity>> Fields { get; } = new();

    public string GetRealClassName()
    {
        return Name.ResolveIfLink().ToString();
    }

    public override string ToString()
    {
        return "Struct: " + GetRealClassName();
    }
}
