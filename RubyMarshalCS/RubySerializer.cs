using System.Collections;
using System.Numerics;
using System.Text;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;
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
            return _context.Create(RubyCodes.Nil);

        if (value is IDynamicProperty dp)
            value = dp.Get();

        var valueType = value.GetType();

        switch (Type.GetTypeCode(valueType))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.Int16:
                return SerializeInt((int)value, flags);
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            {
                var val = Convert.ToUInt64(value);
                if (val <= FixNumMax)
                    return SerializeInt((int)value, flags);

                return SerializeBigInt(val, flags);
            }
            case TypeCode.Int64:
            case TypeCode.Int32:
            {
                long val = Convert.ToInt64(value);
                if (val <= FixNumMax && val >= FixNumMin)
                    return SerializeInt((int)value, flags);

                return SerializeBigInt(val, flags);
            }
            case TypeCode.Boolean:
                return (bool)value ? _context.Create(RubyCodes.True) : _context.Create(RubyCodes.False);
            case TypeCode.Decimal:
                return SerializeFloat(Convert.ToDouble(value), flags);
            case TypeCode.Single:
                return SerializeFloat(Convert.ToDouble(value), flags);
            case TypeCode.Double:
                return SerializeFloat((double)value, flags);
            case TypeCode.String:
            {
                var encoding = flags.HasFlag(CandidateFlags.Character)
                    ? SerializationHelper.ASCII8BitEncoding
                    : Encoding.UTF8;
                return SerializeString((string)value, encoding, flags);
            }
        }

        if (valueType == typeof(SpecialString))
            return SerializeString((SpecialString)value, flags);
        
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
        if (_serializedObjects.ContainsKey(value))
            return _serializedObjects[value];

        var serializer = Activator.CreateInstance(serializerType);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        var ru = (RubyUserDefined)_context.Create(RubyCodes.UserDefined);

        serializerType.GetMethod("Write")!.Invoke(serializer, new[] { value, writer });

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
        if (_serializedObjects.ContainsKey(value))
            return _serializedObjects[value];

        var ro = (RubyObject)_context.Create(RubyCodes.Object);

        _serializedObjects[value] = ro;

        ro.ClassName = SerializeSymbol(rubyObjectTypeName);

        var objectType = value.GetType();
        var info = SerializationHelper.GetTypeCandidateInfo(objectType);
        foreach (var (fieldName, fieldCandidate) in info.FieldCandidates)
        {
            if ((fieldCandidate.Flags & CandidateFlags.InOut) != 0 && !fieldCandidate.Flags.HasFlag(CandidateFlags.Out)  && _settings.ConsiderInOutFields)
                continue;

            object? fieldValue = null;
            if (fieldCandidate.Type == CandidateType.Field)
                fieldValue = objectType.GetField(fieldCandidate.Name)!.GetValue(value);
            else if (fieldCandidate.Type == CandidateType.Property)
                fieldValue = objectType.GetProperty(fieldCandidate.Name)!.GetValue(value);

            ro.Fields.Add(new(SerializeSymbol(fieldName), SerializeValue(fieldValue, fieldCandidate.Flags)));
        }

        if (info.ExtensionDataCandidate != null)
        {
            Dictionary<string, object?>? map;
            if (info.ExtensionDataCandidate.Type == CandidateType.Field)
                map = objectType.GetField(info.ExtensionDataCandidate.Name)!.GetValue(value) as Dictionary<string, object?>;
            else
                map = objectType.GetProperty(info.ExtensionDataCandidate.Name)!.GetValue(value) as Dictionary<string, object?>;

            if (map != null)
                WriteUnknownObjectFields(map, ro);
        }

        return ro;
    }

    private void WriteUnknownObjectFields(Dictionary<string, object?> map, RubyObject ro)
    {
        foreach (var (field, value) in map)
            ro.Fields.Add(new(SerializeSymbol(field), SerializeValue(value)));
    }

    private AbstractEntity SerializeInt(int value, CandidateFlags candidateFlags = CandidateFlags.None)
    {
        var rf = (RubyFixNum)_context.Create(RubyCodes.FixNum);
        rf.Value = value;

        return rf;
    }

    private AbstractEntity SerializeFloat(double value, CandidateFlags flags = CandidateFlags.None)
    {
        if (_serializedFloats.ContainsKey(value))
            return _serializedFloats[value];

        var rf = (RubyFloat)_context.Create(RubyCodes.Float);
        rf.Value = value;

        _serializedFloats[value] = rf;

        return rf;
    }

    private AbstractEntity SerializeBigInt(BigInteger value, CandidateFlags flags = CandidateFlags.None)
    {
        if (_serializedBigInts.ContainsKey(value))
            return _serializedBigInts[value];

        var rbn = (RubyBigNum)_context.Create(RubyCodes.BigNum);
        rbn.Value = value;

        _serializedBigInts[value] = rbn;

        return rbn;
    }

    private AbstractEntity SerializeDictionary(IDictionary value, CandidateFlags flags = CandidateFlags.None)
    {
        if (_serializedObjects.ContainsKey(value))
            return _serializedObjects[value];

        var rh = (RubyHash)_context.Create(RubyCodes.Hash);

        _serializedObjects[value] = rh;

        foreach (DictionaryEntry v in value)
            rh.Pairs.Add(new(SerializeValue(v.Key, flags), SerializeValue(v.Value, flags)));

        if (value is IDefDictionary dd)
            rh.Default = SerializeValue(dd.DefaultValue, flags);

        return rh;
    }

    private AbstractEntity SerializeArray(IList value, CandidateFlags flags = CandidateFlags.None)
    {
        if (_serializedObjects.ContainsKey(value))
            return _serializedObjects[value];

        var ra = (RubyArray)_context.Create(RubyCodes.Array);

        _serializedObjects[value] = ra;

        foreach (var v in value)
            ra.Elements.Add(SerializeValue(v, flags));

        return ra;
    }
    
    private AbstractEntity SerializeString(string value, Encoding encoding, CandidateFlags flags = CandidateFlags.None)
    {
        return SerializeString(encoding.GetBytes(value), encoding);
    }

    private AbstractEntity SerializeString(SpecialString value, CandidateFlags flags = CandidateFlags.None)
    {
        return SerializeString(value.Bytes, value.Encoding);
    }

    private AbstractEntity SerializeString(byte[] value, Encoding encoding, CandidateFlags flags = CandidateFlags.None)
    {
        var asHex = Convert.ToHexString(value);

        if (_serializedStrings.ContainsKey(asHex))
            return _serializedStrings[asHex];

        var rs = (RubyString)_context.Create(RubyCodes.String);

        _serializedStrings[asHex] = rs;

        rs.Bytes = value;

        AbstractEntity? encodingValue = null;

        switch (encoding.CodePage)
        {
            case 1200:
                encodingValue = SerializeString("UTF-16LE");
                break;
            case 1201:
                encodingValue = SerializeString("UTF-16BE");
                break;
            case 1252:
                // ASCII-8bit
                break;
            case 20127:
                encodingValue = _context.Create(RubyCodes.False);
                break;
            case 28591:
                encodingValue = SerializeString("ISO-8859-1");
                break;
            case 65001:
                encodingValue = _context.Create(RubyCodes.True);
                break;
        }

        if (encodingValue != null)
            rs.InstanceVariables.Add(new(SerializeSymbol("E"), encodingValue));

        return rs;
    }

    private AbstractEntity SerializeSymbol(string value)
    {
        if (_serializedSymbols.ContainsKey(value))
            return _serializedSymbols[value];

        var rs = (RubySymbol)_context.Create(RubyCodes.Symbol);
        // TODO: strictly ASCII-8 bit encoding here
        rs.Value = Encoding.UTF8.GetBytes(value);

        _serializedSymbols[value] = rs;

        return rs;
    }
}
