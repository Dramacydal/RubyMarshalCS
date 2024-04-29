using System.Text;

namespace RubyMarshalCS.SpecialTypes;

public class SpecialString
{
    private string _value = "";

    private Encoding _encoding = Encoding.UTF8;

    private byte[] _bytes = Array.Empty<byte>();

    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            _bytes = _encoding.GetBytes(_value);
        }
    }

    public Encoding Encoding
    {
        get => _encoding;
        set
        {
            if (value == _encoding)
                return;
            
            _encoding = value;
            _value = _encoding.GetString(_bytes);
        }
    }

    public byte[] Bytes
    {
        get => _bytes;
        set
        {
            _bytes = value;
            _value = Encoding.GetString(_bytes);
        }
    }

    public SpecialString(string value)
    {
        Value = value;
    }

    public SpecialString()
    {
    }
    
    public SpecialString(byte[] bytes, Encoding encoding)
    {
        _encoding = Encoding;
        Bytes = bytes;
    }

    public static implicit operator string(SpecialString value)
    {
        return value.Value;
    }
    
    public static implicit operator SpecialString(string value)
    {
        return new SpecialString(value);
    }

    public override string ToString() => _value;
}
