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
        if (Code == RubyCodes.SymbolLink)
            return Context.LookupSymbol(this);
        if (Code == RubyCodes.ObjectLink)
            return Context.LookupObject(this);

        return this;
    }
}
