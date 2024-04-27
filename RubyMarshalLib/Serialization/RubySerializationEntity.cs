using RubyMarshalCS.Entities;

namespace RubyMarshalCS.Serialization;

public abstract class RubySerializationEntity
{
    [RubyExtensionData]
    public Dictionary<string, AbstractEntity> _unknownFields = new();
}