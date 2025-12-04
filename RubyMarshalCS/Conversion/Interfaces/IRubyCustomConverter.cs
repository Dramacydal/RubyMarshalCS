using RubyMarshalCS.Entities;

namespace RubyMarshalCS.Conversion.Interfaces;

public interface IRubyCustomConverter
{
    public bool CanConvert(Type type);

    public AbstractEntity Serialize(object value, RubySerializer serializer);

    public object Deserialize(AbstractEntity value, RubyDeserializer deserializer);
    
    object? Cast(object? o);
}
