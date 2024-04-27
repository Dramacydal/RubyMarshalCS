namespace RubyMarshalCS.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RubyDynamicPropertyAttribute : Attribute
{
    public RubyDynamicPropertyAttribute()
    {
    }
}