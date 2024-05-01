namespace RubyMarshalCS.Serialization.Attributes;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class RubyUserSerializerAttribute : Attribute
{
    public Type Type { get; }
    
    public string ContextTag { get; }

    public RubyUserSerializerAttribute(Type type, string contextTag = "")
    {
        Type = type;
        ContextTag = contextTag;
    }
}
