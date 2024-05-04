using RubyMarshalCS.Serialization.Enums;

namespace RubyMarshalCS.Serialization.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RubyPropertyAttribute : Attribute
{
    public RubyPropertyAttribute(string name, CandidateFlags flags = CandidateFlags.None)
    {
        if (!name.StartsWith("@"))
            throw new Exception($"{name} Must start with @");
        Name = name;
        Flags = flags;
    }

    public string Name { get; }

    public CandidateFlags Flags { get; }
}
