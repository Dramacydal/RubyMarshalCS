namespace RubyMarshalCS.Serialization.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RubyDynamicPropertyAttribute : Attribute
{
    public RubyDynamicPropertyAttribute()
    {
    }
}