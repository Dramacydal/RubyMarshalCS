using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubyObjectLink : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.ObjectLink;

    public int ReferenceId { get; set; }

    public override string ToString() => Context.LookupRememberedObject(this).ToString();
}
