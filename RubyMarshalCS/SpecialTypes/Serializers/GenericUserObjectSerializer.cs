using RubyMarshalCS.Serialization;

namespace RubyMarshalCS.SpecialTypes.Serializers;

public class GenericUserObjectSerializer : AbstractRubyUserSerializer<GenericUserObject>
{
    public override void Read(GenericUserObject obj, BinaryReader reader)
    {
        obj.Data = reader.ReadBytes((int)reader.BaseStream.Length);
    }

    public override void Write(GenericUserObject obj, BinaryWriter writer)
    {
        writer.Write(obj.Data);
    }
}