using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyTrue : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.True;

    public override void ReadData(RubyFile r)
    {
    }

    public override void WriteData(RubyFile f)
    {
    }
}