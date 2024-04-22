namespace RubyMarshal.SpecialTypes;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class CustomConverterAttribute : Attribute
{
    public Type Type { get; set; }

    public CustomConverterAttribute(Type t)
    {
        if (!typeof(ICustomConverter).IsAssignableFrom(t))
            throw new Exception($"Type {t} must implement ICustomConverter attribute");

        Type = t;
    }
}