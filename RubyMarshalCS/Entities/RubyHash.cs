using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyHash : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.Hash;

    public List<KeyValuePair<AbstractEntity, AbstractEntity>> Pairs = new();

    public AbstractEntity? Default { get; set; }
}