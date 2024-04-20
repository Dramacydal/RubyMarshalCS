using Newtonsoft.Json;
using RubyMarshal.Enums;

namespace RubyMarshal.Entities;

// [JsonConverter(typeof(EntityConverter))]
public abstract class AbstractEntity
{
    [JsonIgnore]
    public SerializationContext Context { get; set; }

    public abstract RubyCodes Code { get; protected set; }
    
    public abstract void ReadData(BinaryReader r);

    public abstract void WriteData(BinaryWriter w);

    public AbstractEntity ResolveIfLink()
    {
        if (this is RubySymbolLink sl)
            return Context.LookupSymbol(sl);
        if (this is RubyObjectLink ol)
            return Context.LookupObject(ol);

        return this;
    }
}
