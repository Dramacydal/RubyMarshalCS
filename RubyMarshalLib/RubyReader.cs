using Newtonsoft.Json;
using RubyMarshal.Entities;
using RubyMarshal.Exceptions;
using RubyMarshal.Settings;

namespace RubyMarshal;

public class RubyReader
{
    private readonly ReaderSettings _settings;
    
    public AbstractEntity Root { get; set; }

    private SerializationContext Context { get; }

    public RubyReader(ReaderSettings settings = null)
    {
        Context = new SerializationContext(settings);
        _settings = settings ?? _settings;
    }

    public void Read(BinaryReader r)
    {
        var version = r.ReadUInt16();
        if (version != 0x804)
            throw new ValidationException($"Wrong version: {version}");

        Root = Context.Read(r);
    }

    public void ReadJson(StreamReader r)
    {
        // JsonSerializerSettings ss = new()
        // {
        //     TypeNameHandling = TypeNameHandling.All
        // };

        Root = JsonConvert.DeserializeObject<AbstractEntity>(r.ReadToEnd());
    }
}
