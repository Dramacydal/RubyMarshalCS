using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubySymbol : AbstractEntity
{
    public override RubyCodes Code => RubyCodes.Symbol;
    
    public byte[] Value { get; set; }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(Value);
    }
}