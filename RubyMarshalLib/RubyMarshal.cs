using RubyMarshal.Settings;

namespace RubyMarshal;

public class RubyMarshal
{
    public static T Load<T>(string path, ReaderSettings? settings = null)
    {
        return Load<T>(File.OpenRead(path), settings);
    }

    public static T Load<T>(Stream reader, ReaderSettings? settings = null)
    {
        using var binaryReader = new BinaryReader(reader);

        return Load<T>(binaryReader, settings);
    }

    public static T Load<T>(BinaryReader reader, ReaderSettings? settings = null)
    {
        RubyReader rubyReader = new(settings);
        rubyReader.Read(reader);

        return Load<T>(rubyReader, settings);
    }

    public static T Load<T>(RubyReader reader, ReaderSettings? settings = null)
    {
        return RubyConverter.Deserialize<T>(reader.Root, settings);
    }
}
