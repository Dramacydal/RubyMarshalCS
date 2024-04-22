using System.Text;

namespace RubyMarshal.SpecialTypes;

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
            _encoding = value;
            _bytes = _encoding.GetBytes(_value);
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

    public override string ToString() => _value;
}
