using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyObjectLink : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.ObjectLink;

    public int ReferenceId { get; set; }

    public override string ToString() => Context.LookupRememberedObject(this).ToString();
}
