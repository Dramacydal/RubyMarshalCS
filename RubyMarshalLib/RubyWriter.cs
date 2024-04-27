using Newtonsoft.Json;
using RubyMarshalCS.Entities;

namespace RubyMarshalCS;

public class RubyWriter
{
    public void WriteJson(StreamWriter w, AbstractEntity entity)
    {
        JsonSerializerSettings s = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        var str = JsonConvert.SerializeObject(entity, Formatting.Indented, s);
        w.Write(str);
    }
}
