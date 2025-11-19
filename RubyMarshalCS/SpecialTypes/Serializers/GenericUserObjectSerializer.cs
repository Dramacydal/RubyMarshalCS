using RubyMarshalCS.Serialization.Interfaces;

namespace RubyMarshalCS.SpecialTypes.Serializers;

public class GenericUserObjectSerializer : IRubyUserSerializer<GenericUserObject>
{
    public void Read(GenericUserObject obj, BinaryReader reader)
    {
        obj.Data = reader.ReadBytes((int)reader.BaseStream.Length);
    }

    public void Write(GenericUserObject obj, BinaryWriter writer)
    {
        writer.Write(obj.Data);
    }
}