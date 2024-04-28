namespace RubyMarshalCS.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RubyPropertyAttribute : Attribute
{
    public RubyPropertyAttribute(string name, bool isTrash = false)
    {
        if (!name.StartsWith("@"))
            throw new Exception($"{name} Must start with @");
        Name = name;
        IsTrash = isTrash;
    }

    public string Name { get; }
    
    public bool IsTrash { get; }
}