using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyArray : AbstractEntity
{
    public List<AbstractEntity> Elements = new();

    public override RubyCodes Code { get; protected set; } = RubyCodes.Array;
}