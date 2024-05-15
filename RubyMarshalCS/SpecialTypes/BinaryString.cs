using System.Text;

namespace RubyMarshalCS.SpecialTypes;

public class BinaryString
{
    private string _value = "";

    private Encoding _encoding = Encoding.UTF8;

    private byte[] _bytes = Array.Empty<byte>();

    public string Value
    {
        get => _value;
        set => Set(value, _encoding);
    }

    public Encoding Encoding
    {
        get => _encoding;
        set => Set(_value, value);
    }

    public byte[] Bytes => _bytes;

    public BinaryString()
    {
    }

    public BinaryString(string value)
    {
        Set(value, Encoding.UTF8);
    }
    
    public BinaryString(string value, Encoding encoding)
    {
        Set(value, encoding);
    }

    public BinaryString(byte[] bytes, Encoding encoding)
    {
        Set(bytes, encoding);
    }

    public void Set(byte[] bytes, Encoding encoding)
    {
        _encoding = encoding;
        _bytes = bytes;

        _value = Encoding.GetString(bytes);
    }

    public void Set(string value, Encoding encoding)
    {
        _encoding = encoding;
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

    public static implicit operator string(BinaryString value)
    {
        return value.Value;
    }
    
    public static implicit operator BinaryString(string value)
    {
        return new BinaryString(value);
    }

    public override string ToString() => _value;
}
