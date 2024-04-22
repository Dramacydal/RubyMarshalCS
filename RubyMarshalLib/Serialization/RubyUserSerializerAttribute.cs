namespace RubyMarshal.Serialization;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class RubyUserSerializerAttribute : Attribute
{
    public Type? Type { get; set; }

    public RubyUserSerializerAttribute(Type type)
    {
        Type = type;
    }
}
