using RubyMarshalCS.Serialization.Enums;

namespace RubyMarshalCS.Serialization.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RubyPropertyAttribute : Attribute
{
    public RubyPropertyAttribute(string? propertyName, CandidateFlags flags = CandidateFlags.None)
    {
        PropertyName = propertyName;
        Flags = flags;
    }

    public string? PropertyName { get; }

    public CandidateFlags Flags { get; }
}
