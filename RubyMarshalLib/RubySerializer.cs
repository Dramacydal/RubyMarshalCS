using System.Collections;
using System.Text;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;
using RubyMarshalCS.Serialization;
using RubyMarshalCS.SpecialTypes;

namespace RubyMarshalCS;

public class RubySerializer
{
    private readonly SerializationContext _context;

    private RubySerializer()
    {
        _context = new SerializationContext();
    }

    public static AbstractEntity Serialize<T>(T? value)
    {
        var instance = new RubySerializer();

        return instance.SerializeValue(value);
    }

    public static AbstractEntity Serialize(object? value)
    {
        return Serialize<object>(value);
    }

    private AbstractEntity SerializeValue(object? value)
    {
        if (value is IDynamicProperty dp)
            value = dp.Get();
        
        if (value == null)
            return _context.Create(RubyCodes.Nil);
        
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
                return SerializeFloat((float)value);
            case TypeCode.String:
                return SerializeString((string)value, Encoding.UTF8);
        }

        if (valueType == typeof(SpecialString))
            return SerializeString((SpecialString)value);

        if (typeof(IList).IsAssignableFrom(valueType))
            return SerializeArray((IList)value);
        
        if (typeof(IDictionary).IsAssignableFrom(valueType))
            return SerializeDictionary((IDictionary)value);

        var rubyObjectType = SerializationHelper.GetRubyObjectForType(valueType);
        if (!string.IsNullOrEmpty(rubyObjectType))
            return SerializeObject(rubyObjectType, value);
        
        var serializerType = SerializationHelper.GetUserSerializerByType(valueType);
        if (serializerType != null)
            return SerializeUserObject(serializerType, value);

        throw new Exception($"Can't serialize type {valueType}");
    }

    private AbstractEntity SerializeUserObject(Type serializerType, object value)
    {
        if (_serializedObjects.ContainsKey(value))
            return _serializedObjects[value];
        
        var serializer = Activator.CreateInstance(serializerType);

        var method = serializerType.GetMethod("Read")!;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        method.Invoke(serializer, new[] { value, writer });

        var ru = (RubyUserDefined)_context.Create(RubyCodes.UserDefined);

        _serializedObjects[value] = ru;
        
        ru.Bytes = stream.GetBuffer();

        return ru;
    }

    private AbstractEntity SerializeObject(string rubyObjectType, object value)
    {
        if (_serializedObjects.ContainsKey(value))
            return _serializedObjects[value];

        var ro = (RubyObject)_context.Create(RubyCodes.Object);

        _serializedObjects[value] = ro;

        ro.ClassName = SerializeSymbol(rubyObjectType);

        var info = SerializationHelper.GetTypeCandidateInfo(value.GetType());
        foreach (var f in info.FieldCandidates)
        {
            object? fieldValue = null;
            if (f.Value.Type == SerializationHelper.CandidateType.Field)
                fieldValue = value.GetType().GetField(f.Value.Name)!.GetValue(value);
            else if (f.Value.Type == SerializationHelper.CandidateType.Property)
                fieldValue = value.GetType().GetProperty(f.Value.Name)!.GetValue(value);

            ro.Fields.Add(new(SerializeSymbol(f.Key), SerializeValue(fieldValue)));
        }

        return ro;
    }

    private AbstractEntity SerializeInt(int value)
    {
        var rf = (RubyFixNum)_context.Create(RubyCodes.FixNum);
        rf.Value = value;

        return rf;
    }

    private AbstractEntity SerializeFloat(float value)
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

    private Dictionary<string, AbstractEntity> _serializedSymbols = new();
    private Dictionary<string, AbstractEntity> _serializedStrings = new();
    private Dictionary<float, AbstractEntity> _serializedFloats = new();
    private Dictionary<object, AbstractEntity> _serializedObjects = new();

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