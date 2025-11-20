using System.Collections;
using System.Numerics;
using System.Text;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Serialization;
using RubyMarshalCS.Serialization.Enums;
using RubyMarshalCS.Settings;
using RubyMarshalCS.SpecialTypes;
using RubyMarshalCS.SpecialTypes.Interfaces;

namespace RubyMarshalCS;

public class RubySerializer
{
    private const int FixNumMin = -1073741824;
    private const int FixNumMax = 1073741823;
    
    private readonly SerializationContext _context;
    private readonly SerializationSettings _settings;

    private readonly Dictionary<BigInteger, AbstractEntity> _serializedBigInts = new();
    private readonly Dictionary<double, AbstractEntity> _serializedFloats = new();
    private readonly Dictionary<object, AbstractEntity> _serializedObjects = new();
    private readonly Dictionary<string, AbstractEntity> _serializedStrings = new();
    private readonly Dictionary<string, AbstractEntity> _serializedSymbols = new();

    private RubySerializer(SerializationSettings? settings = null)
    {
        _context = new SerializationContext();
        _settings = settings ?? new();
    }

    public static AbstractEntity Serialize<T>(T? value, SerializationSettings? settings = null)
    {
        var instance = new RubySerializer(settings);

        return instance.SerializeValue(value);
    }

    public static AbstractEntity Serialize(object? value, SerializationSettings? settings = null)
    {
        return Serialize<object>(value, settings);
    }

    private AbstractEntity SerializeValue(object? value, CandidateFlags flags = CandidateFlags.None)
    {
        if (value == null)
            return _context.Nil;

        if (value is IDynamicProperty dp)
            value = dp.Get();

        var valueType = value.GetType();

        switch (Type.GetTypeCode(valueType))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.Int16:
                return SerializeInt(Convert.ToInt32(value), flags);
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            {
                var val = Convert.ToUInt64(value);
                if (val <= FixNumMax)
                    return SerializeInt(Convert.ToInt32(value), flags);

                return SerializeBigInt(val, flags);
            }
            case TypeCode.Int64:
            case TypeCode.Int32:
            {
                long val = Convert.ToInt64(value);
                if (val <= FixNumMax && val >= FixNumMin)
                    return SerializeInt(Convert.ToInt32(value), flags);

                return SerializeBigInt(val, flags);
            }
            case TypeCode.Boolean:
                return (bool)value ? _context.True : _context.False;
            case TypeCode.Decimal:
            case TypeCode.Single:
            case TypeCode.Double:
                return SerializeFloat(Convert.ToDouble(value), flags);
            case TypeCode.String:
            {
                var encoding = flags.HasFlag(CandidateFlags.Character)
                    ? SerializationHelper.ASCII8BitEncoding
                    : Encoding.UTF8;
                return SerializeString((string)value, encoding, flags);
            }
        }

        if (valueType == typeof(BinaryString))
            return SerializeString((BinaryString)value, flags);
        
        if (valueType == typeof(BigInteger))
            return SerializeBigInt((BigInteger)value, flags);

        var customConverter = SerializationHelper.GetBackConverter(value);
        if (customConverter != null)
            return SerializeValue(customConverter.ConvertBack(value), flags);
        
        if (typeof(IList).IsAssignableFrom(valueType))
            return SerializeArray((IList)value, flags);

        if (typeof(IDictionary).IsAssignableFrom(valueType))
            return SerializeDictionary((IDictionary)value, flags);

        var serializerType = SerializationHelper.GetUserSerializerByType(valueType, _settings.ContextTag);
        if (serializerType != null)
            return SerializeUserObject(serializerType, value, flags);

        var rubyObjectTypeName = SerializationHelper.GetRubyObjectTypeNameForType(valueType, _settings.ContextTag);
        if (!string.IsNullOrEmpty(rubyObjectTypeName))
            return SerializeObject(rubyObjectTypeName, value, flags);

        throw new Exception($"Can't serialize type {valueType}");
    }

    private AbstractEntity SerializeUserObject(Type serializerType, object value, CandidateFlags flags = CandidateFlags.None)
    {
        if (_serializedObjects.TryGetValue(value, out var o))
            return o;

        var serializer = Activator.CreateInstance(serializerType);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        var ru = _context.Create<RubyUserDefined>();

        serializerType.GetMethod("Write")!.Invoke(serializer, [value, writer]);

        if (value is GenericUserObject go)
            ru.ClassName = SerializeSymbol(go.Name);
        else
            ru.ClassName = SerializeSymbol(SerializationHelper.GetRubyObjectTypeNameForType(value.GetType(), _settings.ContextTag)!);

        ru.Bytes = stream.GetTrimmedBuffer();

        _serializedObjects[value] = ru;

        return ru;
    }

    private AbstractEntity SerializeObject(string rubyObjectTypeName, object value, CandidateFlags flags)
    {
        if (_serializedObjects.TryGetValue(value, out var o))
            return o;

        var ro = _context.Create<RubyObject>();

        _serializedObjects[value] = ro;

        ro.ClassName = SerializeSymbol(rubyObjectTypeName);

        var objectType = value.GetType();
        var info = SerializationHelper.GetTypeCandidateInfo(objectType);
        
        info.OnSerializingMethod?.Invoke(value, [ro]);
        
        foreach (var (fieldName, fieldCandidate) in info.FieldCandidates)
        {
            if ((fieldCandidate.Flags & CandidateFlags.InOut) != 0 && !fieldCandidate.Flags.HasFlag(CandidateFlags.Out)  && _settings.ConsiderInOutFields)
                continue;

            var fieldValue = fieldCandidate.GetValue(value);

            ro.Fields.Add(new(SerializeSymbol(fieldName), SerializeValue(fieldValue, fieldCandidate.Flags)));
        }

        if (info.ExtensionDataCandidate?.GetValue(value) is Dictionary<string, object?> map)
            WriteUnknownObjectFields(map, ro);

        info.OnSerializedMethod?.Invoke(value, [ro]);

        return ro;
    }

    private void WriteUnknownObjectFields(Dictionary<string, object?> map, RubyObject ro)
    {
        foreach (var (field, value) in map)
            ro.Fields.Add(new(SerializeSymbol(field), SerializeValue(value)));
    }

    private AbstractEntity SerializeInt(int value, CandidateFlags candidateFlags = CandidateFlags.None)
    {
        var rf = _context.Create<RubyFixNum>();
        rf.Value = value;

        return rf;
    }

    private AbstractEntity SerializeFloat(double value, CandidateFlags flags = CandidateFlags.None)
    {
        if (_serializedFloats.TryGetValue(value, out var f))
            return f;

        var rf = _context.Create<RubyFloat>();
        rf.Value = value;

        _serializedFloats[value] = rf;

        return rf;
    }

    private AbstractEntity SerializeBigInt(BigInteger value, CandidateFlags flags = CandidateFlags.None)
    {
        if (_serializedBigInts.TryGetValue(value, out var i))
            return i;

        var rbn = _context.Create<RubyBigNum>();
        rbn.Value = value;

        _serializedBigInts[value] = rbn;

        return rbn;
    }

    private AbstractEntity SerializeDictionary(IDictionary value, CandidateFlags flags = CandidateFlags.None)
    {
        if (_serializedObjects.TryGetValue(value, out var o))
            return o;

        var rh = _context.Create<RubyHash>();

        _serializedObjects[value] = rh;

        foreach (DictionaryEntry v in value)
            rh.Pairs.Add(new(SerializeValue(v.Key, flags), SerializeValue(v.Value, flags)));

        if (value is IDefDictionary dd)
            rh.Default = SerializeValue(dd.DefaultValue, flags);

        return rh;
    }

    private AbstractEntity SerializeArray(IList value, CandidateFlags flags = CandidateFlags.None)
    {
        if (_serializedObjects.TryGetValue(value, out var o))
            return o;

        var ra = _context.Create<RubyArray>();

        _serializedObjects[value] = ra;

        foreach (var v in value)
            ra.Elements.Add(SerializeValue(v, flags));

        return ra;
    }
    
    private AbstractEntity SerializeString(string value, Encoding encoding, CandidateFlags flags = CandidateFlags.None)
    {
        return SerializeString(encoding.GetBytes(value), encoding, flags);
    }

    private AbstractEntity SerializeString(BinaryString value, CandidateFlags flags = CandidateFlags.None)
    {
        return SerializeString(value.Bytes, value.Encoding, flags);
    }

    private AbstractEntity SerializeString(byte[] value, Encoding? encoding, CandidateFlags flags = CandidateFlags.None)
    {
        if (flags.HasFlag(CandidateFlags.Compressed))
            value = RubyDeflate.Deflate(value);
            
        var asHex = Convert.ToHexString(value) + (encoding?.ToString() ?? "");

        if (_serializedStrings.TryGetValue(asHex, out var s))
            return s;

        var rs = _context.Create<RubyString>();

        _serializedStrings[asHex] = rs;

        rs.Bytes = value;

        AbstractEntity? encodingValue = null;

        if (encoding != null)
        {
            switch (encoding.CodePage)
            {
                case 1200:
                    encodingValue = SerializeString("UTF-16LE");
                    break;
                case 1201:
                    encodingValue = SerializeString("UTF-16BE");
                    break;
                case 1252:
                    encodingValue = SerializeString("ASCII-8BIT");
                    break;
                case 20127:
                    encodingValue = _context.False;
                    break;
                case 28591:
                    encodingValue = SerializeString("ISO-8859-1");
                    break;
                case 65001:
                    encodingValue = _context.True;
                    break;
                default:
                    throw new Exception("Unknown encoding code page: " + encoding.CodePage);
            }
        }

        if (encodingValue != null)
            rs.InstanceVariables.Add(new(SerializeSymbol("E"), encodingValue));

        return rs;
    }

    private AbstractEntity SerializeSymbol(string value)
    {
        if (_serializedSymbols.TryGetValue(value, out var symbol))
            return symbol;

        var rs = _context.Create<RubySymbol>();
        // TODO: strictly ASCII-8 bit encoding here
        rs.Value = Encoding.UTF8.GetBytes(value);

        _serializedSymbols[value] = rs;

        return rs;
    }
}
