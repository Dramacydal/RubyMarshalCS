namespace RubyMarshal.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RubyPropertyAttribute : Attribute
{
    public string? Name { get; set; }
}