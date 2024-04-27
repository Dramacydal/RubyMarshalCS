using Newtonsoft.Json;
using RubyMarshalCS.Entities;

namespace RubyMarshalCS;

public class RubyReader
{
    public AbstractEntity? ReadJson(StreamReader r)
    {
        // JsonSerializerSettings ss = new()
        // {
        //     TypeNameHandling = TypeNameHandling.All
        // };

        return JsonConvert.DeserializeObject<AbstractEntity>(r.ReadToEnd());
    }
}
