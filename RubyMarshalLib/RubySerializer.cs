using System.Collections;
using System.Text;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;
using RubyMarshalCS.Serialization;
using RubyMarshalCS.Settings;
using RubyMarshalCS.SpecialTypes;
using RubyMarshalCS.SpecialTypes.Interfaces;

namespace RubyMarshalCS;

public class RubySerializer
{
    private readonly SerializationContext _context;

    private readonly Dictionary<string, AbstractEntity> _serializedSymbols = new();
    private readonly Dictionary<string, AbstractEntity> _serializedStrings = new();
    private readonly Dictionary<double, AbstractEntity> _serializedFloats = new();
    private readonly Dictionary<object, AbstractEntity> _serializedObjects = new();
    private readonly SerializationSettings _settings;

    private RubySerializer(SerializationSettings? settings=null)
    {
        _context = new SerializationContext();
        _settings = settings ?? new();
    }

    public static AbstractEntity Serialize<T>(T? value, SerializationSettings? settings=null)
    {
        var instance = new RubySerializer(settings);

        return instance.SerializeValue(value);
    }

    public static AbstractEntity Serialize(object? value, SerializationSettings? settings=null)
    {
        return Serialize<object>(value, settings);
    }

    private AbstractEntity SerializeValue(object? value)
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
            case TypeCode.UInt32:
            case TypeCode.Int32:
            case TypeCode.UInt64:
            case TypeCode.Int64:
            case TypeCode.Decimal:
                return SerializeInt((int)value);
            case TypeCode.Boolean:
                return (bool)value ? _context.Create(RubyCodes.True) : _context.Create(RubyCodes.False);
            case TypeCode.Single:
            case TypeCode.Double:
                return SerializeFloat((double)value);
            case TypeCode.String:
                return SerializeString((string)value, Encoding.UTF8);
        }

        if (valueType == typeof(SpecialString))
            return SerializeString((SpecialString)value);

        if (typeof(IList).IsAssignableFrom(valueType))
            return SerializeArray((IList)value);
        
        if (typeof(IDictionary).IsAssignableFrom(valueType))
            return SerializeDictionary((IDictionary)value);

        var serializerType = SerializationHelper.GetUserSerializerByType(valueType);
        if (serializerType != null)
            return SerializeUserObject(serializerType, value);

        var rubyObjectTypeName = SerializationHelper.GetRubyObjectTypeNameForType(valueType);
        if (!string.IsNullOrEmpty(rubyObjectTypeName))
            return SerializeObject(rubyObjectTypeName, value);
        
        throw new Exception($"Can't serialize type {valueType}");
    }

    private AbstractEntity SerializeUserObject(Type serializerType, object value)
    {
        if (_serializedObjects.ContainsKey(value))
            return _serializedObjects[value];

        var serializer = Activator.CreateInstance(serializerType);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        var ru = (RubyUserDefined)_context.Create(RubyCodes.UserDefined);

        serializerType.GetMethod("Write")!.Invoke(serializer, new[] { value, writer });

        var rubyObjectTypeName = (string)serializerType.GetMethod("GetObjectName")!.Invoke(serializer, new[] { value })!;
        ru.ClassName = SerializeSymbol(rubyObjectTypeName);

        var bytes = stream.GetBuffer();
        if (stream.Length < bytes.Length)
            Array.Resize(ref bytes, (int)stream.Length);
        
        ru.Bytes = bytes;

        _serializedObjects[value] = ru;
        
        return ru;
    }

    private AbstractEntity SerializeObject(string rubyObjectTypeName, object value)
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
            if (fieldCandidate.IsTrash && !_settings.WriteTrashProperties)
                continue;

            object? fieldValue = null;
            if (fieldCandidate.Type == SerializationHelper.CandidateType.Field)
                fieldValue = objectType.GetField(fieldCandidate.Name)!.GetValue(value);
            else if (fieldCandidate.Type == SerializationHelper.CandidateType.Property)
                fieldValue = objectType.GetProperty(fieldCandidate.Name)!.GetValue(value);

            ro.Fields.Add(new(SerializeSymbol(fieldName), SerializeValue(fieldValue)));
        }

        if (info.ExtensionDataCandidate != null)
        {
            Dictionary<string, object?>? map;
            if (info.ExtensionDataCandidate.Type == SerializationHelper.CandidateType.Field)
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

    private AbstractEntity SerializeInt(int value)
    {
        var rf = (RubyFixNum)_context.Create(RubyCodes.FixNum);
        rf.Value = value;

        return rf;
    }

    private AbstractEntity SerializeFloat(double value)
    {
        if (_serializedFloats.ContainsKey(value))
            return _serializedFloats[value];
        
        var rf = (RubyFloat)_context.Create(RubyCodes.Float);
        rf.Value = value;
        
        _serializedFloats[value] = rf;
        
        return rf;
    }

    private AbstractEntity SerializeDictionary(IDictionary value)
    {
        if (_serializedObjects.ContainsKey(value))
            return _serializedObjects[value];
        
        var rh = (RubyHash)_context.Create(RubyCodes.Hash);
        
        _serializedObjects[value] = rh;

        foreach (DictionaryEntry v in value)
            rh.Pairs.Add(new(SerializeValue(v.Key), SerializeValue(v.Value)));
        
        return rh;
    }

    private AbstractEntity SerializeArray(IList value)
    {
        if (_serializedObjects.ContainsKey(value))
            return _serializedObjects[value];
        
        var ra = (RubyArray)_context.Create(RubyCodes.Array);
        
        _serializedObjects[value] = ra;
        
        foreach (var v in value)
            ra.Elements.Add(SerializeValue(v));
        
        return ra;
    }

    private AbstractEntity SerializeString(string value, Encoding encoding)
    {
        return SerializeString(encoding.GetBytes(value));
    }

    private AbstractEntity SerializeString(SpecialString value)
    {
        return SerializeString(value.Bytes);
    }

    private AbstractEntity SerializeString(byte[] value)
    {
        var asHex = Convert.ToHexString(value);
        
        if (_serializedStrings.ContainsKey(asHex))
            return _serializedStrings[asHex];
        
        var iv = (RubyInstanceVariable)_context.Create(RubyCodes.InstanceVar, true);

        _serializedStrings[asHex] = iv;
        
        var str = (RubyString)_context.Create(RubyCodes.String);
        str.Bytes = value;

        var rs = SerializeSymbol("E");
        
        iv.Object = str;
        iv.Variables.Add(new(rs, _context.Create(RubyCodes.True)));
        // True - UTF-8
        // False - ASCII
        // string - other encoding name

        return iv;
    }

    private AbstractEntity SerializeSymbol(string value)
    {
        if (_serializedSymbols.ContainsKey(value))
            return _serializedSymbols[value];
        
        var rs = (RubySymbol)_context.Create(RubyCodes.Symbol);
        rs.Name = value;

        _serializedSymbols[value] = rs;
        
        return rs;
    }
}