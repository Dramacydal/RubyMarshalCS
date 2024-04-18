
using RPGMakerVXConverterLib.Enums;

namespace RPGMakerVXConverterLib.Entities;

public class RubyFalse : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.False;

    public override void ReadData(RubyFile r)
    {
    }

    public override void WriteData(RubyFile f)
    {
    }
}