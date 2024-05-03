using RubyMarshalCS.Entities;
using RubyMarshalCS.Settings;

namespace RubyMarshalCS;

public static class RubyMarshal
{
    public static T? Load<T>(byte[] bytes, SerializationSettings? settings = null)
    {
        return RubyDeserializer.Deserialize<T>(Load(bytes, settings), settings);
    }

    public static T? Load<T>(string path, SerializationSettings? settings = null)
    {
        return RubyDeserializer.Deserialize<T>(Load(path, settings), settings);
    }

    public static T? Load<T>(Stream stream, SerializationSettings? settings = null)
    {
        return RubyDeserializer.Deserialize<T>(Load(stream, settings), settings);
    }

    public static T? Load<T>(BinaryReader reader, SerializationSettings? settings = null)
    {
        return RubyDeserializer.Deserialize<T>(Load(reader, settings), settings);
    }

    public static AbstractEntity Load(byte[] bytes, SerializationSettings? settings = null)
    {
        using var stream = new MemoryStream(bytes);

        return Load(stream, settings);
    }

    public static AbstractEntity Load(string path, SerializationSettings? settings = null)
    {
        return Load(File.OpenRead(path), settings);
    }

    public static AbstractEntity Load(Stream stream, SerializationSettings? settings = null)
    {
        using var reader = new BinaryReader(stream);

        return Load(reader, settings);
    }

    public static AbstractEntity Load(BinaryReader reader, SerializationSettings? settings = null)
    {
        var rr = new RubyReader(reader, settings);

        return rr.Read();
    }

    public static void Dump<T>(string path, T? obj, SerializationSettings? settings = null)
    {
        var entity = RubySerializer.Serialize(obj, settings);
        
        Dump(path, entity);
    }

    public static void Dump<T>(Stream stream, T? obj, SerializationSettings? settings = null)
    {
        var entity = RubySerializer.Serialize(obj, settings);
        
        Dump(stream, entity);
    }
    
    public static void Dump<T>(BinaryWriter writer, T? obj, SerializationSettings? settings = null)
    {
        var entity = RubySerializer.Serialize(obj, settings);
        
        Dump(writer, entity);
    }

    public static byte[] Dump<T>(T? obj, SerializationSettings? settings = null)
    {
        var entity = RubySerializer.Serialize(obj, settings);
        
        return Dump(entity);
    }

    public static void Dump(string path, AbstractEntity entity)
    {
        Dump(File.Open(path, FileMode.Create), entity);
    }

    public static void Dump(Stream stream, AbstractEntity entity)
    {
        using var writer = new BinaryWriter(stream);

        Dump(writer, entity);
    }

    public static void Dump(BinaryWriter writer, AbstractEntity entity)
    {
        var rw = new RubyWriter(writer);

        rw.Write(entity);
    }

    public static byte[] Dump(AbstractEntity entity)
    {
        using var stream = new MemoryStream();

        Dump(stream, entity);

        return stream.GetTrimmedBuffer();
    }
}
