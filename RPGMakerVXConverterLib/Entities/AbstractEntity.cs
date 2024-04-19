using Newtonsoft.Json;
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

// [JsonConverter(typeof(EntityConverter))]
public abstract class AbstractEntity
{
    [JsonIgnore]
    public SerializationContext Context { get; set; }

    public abstract RubyCodes Code { get; protected set; }
    
    public abstract void ReadData(BinaryReader r);

    public abstract void WriteData(BinaryWriter w);
}
