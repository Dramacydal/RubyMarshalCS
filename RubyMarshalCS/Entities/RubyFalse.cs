using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyFalse : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.False;
}
