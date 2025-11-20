using System.Text;
using RubyMarshalCS.Serialization;

namespace RubyMarshalCS.SpecialTypes;

public class BinaryString
{
    private string _value = "";

    private Encoding? _encoding;

    private byte[] _bytes = [];

    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            _bytes = (_encoding ?? SerializationHelper.ASCII8BitEncoding).GetBytes(value);
        }
    }

    public Encoding? Encoding
    {
        get => _encoding;
        set
        {
            _encoding = value;
            _bytes = (_encoding ?? SerializationHelper.ASCII8BitEncoding).GetBytes(Value);
        }
    }

    public byte[] Bytes => _bytes;

    public BinaryString(string value)
    {
        _value = value;
        _encoding = null;
        _bytes = SerializationHelper.ASCII8BitEncoding.GetBytes(_value);
    }
    
    public BinaryString(string value, Encoding? encoding)
    {
        _encoding = encoding;
        Value = value;
    }

    public BinaryString(byte[] bytes, Encoding? encoding)
    {
        _bytes = bytes;
        _encoding = encoding;
        _value = (_encoding ?? SerializationHelper.ASCII8BitEncoding).GetString(_bytes);
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
