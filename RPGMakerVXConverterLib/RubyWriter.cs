using Newtonsoft.Json;
using RPGMakerVXConverterLib.Entities;

namespace RPGMakerVXConverterLib;

public class RubyWriter
{
    public AbstractEntity Root { get; set; }
    
    private SerializationContext Context { get; }

    public RubyWriter()
    {
        Context = new SerializationContext();
    }

    public void Write(BinaryWriter w)
    {
        w.Write((ushort)0x804);

        Context.Write(w, Root);
    }
    
    public void WriteJson(StreamWriter w)
    {
        JsonSerializerSettings s = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        var str = JsonConvert.SerializeObject(Root, Formatting.Indented, s);
        w.Write(str);
    }
}
