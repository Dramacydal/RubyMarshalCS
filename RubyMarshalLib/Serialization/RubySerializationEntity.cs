using RubyMarshal.Entities;

namespace RubyMarshal.Serialization;

public abstract class RubySerializationEntity
{
    [RubyExtensionData]
    public Dictionary<string, AbstractEntity> _unknownFields = new();
}