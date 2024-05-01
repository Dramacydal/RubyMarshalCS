namespace RubyMarshalCS.Serialization.Attributes;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class RubyObjectAttribute : Attribute
{
    public string Name { get; }

    public string ContextTag { get; }
    
    public RubyObjectAttribute(string name, string contextTag = "")
    {
        Name = name;
        ContextTag = contextTag;
    }
}
