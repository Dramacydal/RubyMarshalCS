namespace RubyMarshal.Serialization;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public class RubyObjectAttribute : Attribute
{
    public RubyObjectAttribute(string name)
    {
        Name = name;
    }
    
    public string? Name { get; set; }
}