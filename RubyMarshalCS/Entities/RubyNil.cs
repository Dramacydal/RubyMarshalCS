using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyNil : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.Nil;
}