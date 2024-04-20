namespace RubyMarshal.Serialization;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class RubyObjectAttribute : Attribute
{
    public string? Name { get; set; }
}