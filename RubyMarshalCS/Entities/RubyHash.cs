using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyHash : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.Hash;

    public List<KeyValuePair<AbstractEntity, AbstractEntity>> Pairs = new();

    public AbstractEntity? Default { get; set; }
}