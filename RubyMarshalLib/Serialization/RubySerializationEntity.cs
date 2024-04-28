using RubyMarshalCS.Serialization.Attributes;

namespace RubyMarshalCS.Serialization;

public abstract class RubySerializationEntity
{
    [RubyExtensionData]
    public Dictionary<string, object?> _unknownFields = new();
}