using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public abstract class AbstractEntity
{
    public abstract RubyCodes Code { get; protected set; }
    
    public abstract void ReadData(RubyFile f);

    public abstract void WriteData(RubyFile f);
}
