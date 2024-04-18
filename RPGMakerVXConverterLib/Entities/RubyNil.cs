using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyNil : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.Nil;

    public override void ReadData(RubyFile r)
    {
    }

    public override void WriteData(RubyFile f)
    {
    }
}