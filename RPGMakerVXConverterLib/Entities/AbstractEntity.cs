using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public abstract class AbstractEntity
{
    public EntityFactory Factory { get; set; }

    public abstract RubyCodes Code { get; protected set; }
    
    public abstract void ReadData(BinaryReader r);

    public abstract void WriteData(BinaryWriter w);
}
