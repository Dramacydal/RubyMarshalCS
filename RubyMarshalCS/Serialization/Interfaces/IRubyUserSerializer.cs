namespace RubyMarshalCS.Serialization.Interfaces;

public interface IRubyUserSerializer<T>
{
    public void Read(T obj, BinaryReader reader);
    public void Write(T obj, BinaryWriter writer);
}
