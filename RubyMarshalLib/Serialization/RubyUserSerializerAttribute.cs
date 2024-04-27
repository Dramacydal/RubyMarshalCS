namespace RubyMarshalCS.Serialization;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class RubyUserSerializerAttribute : Attribute
{
    public Type Type { get; }

    public RubyUserSerializerAttribute(Type type)
    {
        Type = type;
    }
}
