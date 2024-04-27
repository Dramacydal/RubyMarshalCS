using RubyMarshalCS.Entities;
using RubyMarshalCS.Settings;

namespace RubyMarshalCS;

public static class RubyMarshal
{
    private const ushort HeaderMagic = 0x804;
    
    public static T? Load<T>(string path, ReaderSettings? settings = null)
    {
        using var reader = new BinaryReader(File.OpenRead(path));

        return Load<T>(reader, settings);
    }

    public static T? Load<T>(BinaryReader reader, ReaderSettings? settings = null)
    {
        return RubyDeserializer.Deserialize<T>(Load(reader, settings), settings);
    }

    public static AbstractEntity Load(string path, ReaderSettings? settings = null)
    {
        using var reader = new BinaryReader(File.OpenRead(path));

        return Load(reader, settings);
    }

    public static AbstractEntity Load(BinaryReader reader, ReaderSettings? settings = null)
    {
        var version = reader.ReadUInt16();
        if (version != HeaderMagic)
            throw new Exception($"Wrong ruby serializer version: {version}");

        return new SerializationContext(settings).Read(reader);
    }

    public static void Dump<T>(string path, T? obj)
    {
        var entity = RubySerializer.Serialize(obj);
        
        Dump(path, entity);
    }

    public static void Dump<T>(BinaryWriter writer, T? obj)
    {
        var entity = RubySerializer.Serialize(obj);
        
        Dump(writer, entity);
    }
    
    public static void Dump(string path, AbstractEntity entity)
    {
        using var writer = new BinaryWriter(File.Open(path, FileMode.Create));

        Dump(writer, entity);
    }

    public static void Dump(BinaryWriter writer, AbstractEntity entity)
    {
        writer.Write(HeaderMagic);

        new SerializationContext().Write(writer, entity);
    }
}
