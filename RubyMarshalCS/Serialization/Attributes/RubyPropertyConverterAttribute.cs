namespace RubyMarshalCS.Serialization.Attributes;

/// <summary>Applies an <see cref="Interfaces.IRubyPropertyConverter"/> to a property marked with <see cref="RubyPropertyAttribute"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RubyPropertyConverterAttribute : Attribute
{
    public RubyPropertyConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }

    public Type ConverterType { get; }
}
