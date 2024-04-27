using Newtonsoft.Json;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

// [JsonConverter(typeof(EntityConverter))]
public abstract class AbstractEntity
{
    [JsonIgnore]
    public SerializationContext Context { get; set; }

    public abstract RubyCodes Code { get; protected set; }
    
    public abstract void ReadData(BinaryReader reader);

    public abstract void WriteData(BinaryWriter writer);

    public AbstractEntity ResolveIfLink()
    {
        return Code switch
        {
            RubyCodes.SymbolLink => Context.LookupSymbol(this),
            RubyCodes.ObjectLink => Context.LookupObject(this),
            _ => this
        };
    }
}
