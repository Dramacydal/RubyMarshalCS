using RubyMarshalCS.Entities;
using RubyMarshalCS.Settings;

namespace RubyMarshalCS;

public static class RubyMarshal
{
    public static T? Load<T>(byte[] bytes, DeserializationSettings? settings = null)
    {
        return new RubyDeserializer(settings).Deserialize<T>(Load(bytes, settings));
    }

    public static T? Load<T>(string path, DeserializationSettings? settings = null)
    {
        return new RubyDeserializer(settings).Deserialize<T>(Load(path, settings));
    }

    public static T? Load<T>(Stream stream, DeserializationSettings? settings = null)
    {
        return new RubyDeserializer(settings).Deserialize<T>(Load(stream, settings));
    }

    public static T? Load<T>(BinaryReader reader, DeserializationSettings? settings = null)
    {
        return new RubyDeserializer(settings).Deserialize<T>(Load(reader, settings));
    }

    public static AbstractEntity Load(byte[] bytes, DeserializationSettings? settings = null)
    {
        using var stream = new MemoryStream(bytes);

        return Load(stream, settings);
    }

    public static AbstractEntity Load(string path, DeserializationSettings? settings = null)
    {
        return Load(File.OpenRead(path), settings);
    }

    public static AbstractEntity Load(Stream stream, DeserializationSettings? settings = null)
    {
        using var reader = new BinaryReader(stream);

        return Load(reader, settings);
    }

    public static AbstractEntity Load(BinaryReader reader, DeserializationSettings? settings = null)
    {
        var rr = new RubyReader(reader, settings);

        return rr.Read();
    }

    public static void Dump(string path, object? obj, SerializationSettings? settings = null)
    {
        var entity = new RubySerializer(settings).Serialize(obj);
        
        Dump(path, entity);
    }

    public static void Dump(Stream stream, object? obj, SerializationSettings? settings = null)
    {
        var entity = new RubySerializer(settings).Serialize(obj);
        
        Dump(stream, entity);
    }
    
    public static void Dump(BinaryWriter writer, object? obj, SerializationSettings? settings = null)
    {
        var entity = new RubySerializer(settings).Serialize(obj);
        
        Dump(writer, entity);
    }

    public static byte[] Dump(object? obj, SerializationSettings? settings = null)
    {
        var entity = new RubySerializer(settings).Serialize(obj);
        
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
