namespace RubyMarshal.Serialization;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class RubyCustomSerializerAttribute : Attribute
{
    public Type? Type { get; set; }

    public RubyCustomSerializerAttribute(Type type)
    {
        this.Type = type;
    }
}
