using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubySymbolLink : AbstractEntity
{
    public override RubyCodes Code { get; protected set; } = RubyCodes.SymbolLink;

    public int ReferenceId { get; set; }

    public override string ToString() => Context.LookupRememberedSymbol(this).ToString();
}
