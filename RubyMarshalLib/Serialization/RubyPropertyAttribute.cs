namespace RubyMarshalCS.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RubyPropertyAttribute : Attribute
{
    public RubyPropertyAttribute(string name)
    {
        if (!name.StartsWith("@"))
            throw new Exception($"{name} Must start with @");
        Name = name;
    }
    
    public string Name { get; }
}