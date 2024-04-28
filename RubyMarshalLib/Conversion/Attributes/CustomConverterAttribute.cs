using RubyMarshalCS.Conversion.Interfaces;

namespace RubyMarshalCS.Conversion.Attributes;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class CustomConverterAttribute : Attribute
{
    public Type Type { get; }

    public CustomConverterAttribute(Type t)
    {
        if (!typeof(ICustomConverter).IsAssignableFrom(t))
            throw new Exception($"Type {t} must implement ICustomConverter attribute");

        Type = t;
    }
}