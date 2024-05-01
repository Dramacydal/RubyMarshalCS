using RubyMarshalCS.Serialization.Interfaces;

namespace RubyMarshalCS.Serialization;

public abstract class AbstractRubyUserSerializer<T> : IRubyUserSerializer<T>
{
    public abstract void Read(T obj, BinaryReader reader);
    public abstract void Write(T obj, BinaryWriter writer);
}
