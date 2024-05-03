using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyTrue : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.True;
}
