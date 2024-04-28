using RubyMarshalCS.Serialization.Attributes;
using RubyMarshalCS.SpecialTypes.Serializers;

namespace RubyMarshalCS.SpecialTypes;

[RubyUserSerializer(typeof(GenericUserObjectSerializer))]
public class GenericUserObject
{
    public string Name { get; set; }
    public byte[] Data { get; set; }
}
