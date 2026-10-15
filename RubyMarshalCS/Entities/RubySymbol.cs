using System.Text;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public class RubySymbol : AbstractEntity
{
    private byte[] _value;
    private string? _text;

    public override RubyCodes Code => RubyCodes.Symbol;

    public byte[] Value
    {
        get => _value;
        set
        {
            _value = value;
            _text = null;
        }
    }

    // a symbol is turned into text once for every field of every object that names it
    public override string ToString()
    {
        return _text ??= Encoding.UTF8.GetString(_value);
    }
}
