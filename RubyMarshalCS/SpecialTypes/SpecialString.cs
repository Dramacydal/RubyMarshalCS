using System.Text;

namespace RubyMarshalCS.SpecialTypes;

public class SpecialString
{
    private string _value = "";

    private Encoding _encoding = Encoding.UTF8;

    private byte[] _bytes = Array.Empty<byte>();

    public string Value => _value;

    public Encoding Encoding => _encoding;

    public byte[] Bytes => _bytes;

    public SpecialString()
    {
    }

    public SpecialString(string value)
    {
        Set(value, Encoding.UTF8);
    }

    public SpecialString(byte[] bytes, Encoding encoding)
    {
        Set(bytes, encoding);
    }

    public void Set(byte[] bytes, Encoding encoding)
    {
        _encoding = Encoding;
        _bytes = bytes;

        _value = Encoding.GetString(bytes);
    }

    public void Set(string value, Encoding encoding)
    {
        _encoding = Encoding;
        _value = value;

        _bytes = Encoding.GetBytes(_value);
    }

    public void Reencode(Encoding encoding)
    {
        _encoding = encoding;

        _bytes = encoding.GetBytes(_value);
    }

    public void ChangeEncoding(Encoding encoding)
    {
        _encoding = encoding;

        _value = encoding.GetString(_bytes);
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
