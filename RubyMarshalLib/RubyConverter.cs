using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using RubyMarshal.Entities;
using RubyMarshal.Enums;
using RubyMarshal.Serialization;
using RubyMarshal.Settings;
using RubyMarshal.SpecialTypes;

namespace RubyMarshal;

public class RubyConverter
{
    private readonly ReaderSettings _settings;

    private static Dictionary<string, Type> _rubyObjectsClassMap = new();

    private static Dictionary<Type, Type> _userSerializersByType = new();

    private Dictionary<object, object> _objectConversionMap = new();

    private RubyConverter(ReaderSettings? settings = null)
    {
        _settings = settings ?? new();
    }

    static RubyConverter()
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        foreach (var t in a.GetTypes())
        foreach (var attr in t.GetCustomAttributes())
        {
            if (attr.GetType() == typeof(RubyObjectAttribute))
                RegisterRubyObject(((RubyObjectAttribute)attr).Name!, t);
            else if (attr.GetType() == typeof(RubyUserSerializerAttribute))
                RegisterUserObjectSerializer(t, ((RubyUserSerializerAttribute)attr).Type);
            else if (attr.GetType() == typeof(CustomConverterAttribute))
                RegisterCustomConverter(((CustomConverterAttribute)attr).Type);
        }
    }

    private static void RegisterCustomConverter(Type type)
    {
        SerializationHelper.RegisterCustomConverter((ICustomConverter)Activator.CreateInstance(type)!);
    }

    public static void RegisterRubyObject(string name, Type type)
    {
        if (_rubyObjectsClassMap.ContainsKey(name))
            throw new Exception($"Ruby object [{name}] already registered");

        _rubyObjectsClassMap[name] = type;
    }

    public static void RegisterUserObjectSerializer(Type type, Type serializer)
    {
        if (_userSerializersByType.ContainsKey(type))
            throw new Exception($"User object serializer for type [{type}] already registered");

        // TODO: by default read/write bytes ?
        var found = false;
        foreach (var i in serializer.GetInterfaces())
        {
            if (i.IsGenericType)
            {
                var g = i.GetGenericTypeDefinition();
                if (g == typeof(IRubyUserSerializer<>))
                {
                    found = true;
                    break;
                }
            }
        }

        if (!found)
            throw new Exception(
                $"User object serializer [{serializer}] does not implement ICustomRubySerializer<> interface");

        _userSerializersByType[type] = serializer;
    }

    public static T? Deserialize<T>(string path, ReaderSettings? settings = null)
    {
        using var file = File.OpenRead(path);
        using var reader = new BinaryReader(file);

        return Deserialize<T>(reader, settings);;
    }

    public static T? Deserialize<T>(BinaryReader reader, ReaderSettings? settings = null)
    {
        var rubyReader = new RubyReader(settings);
        rubyReader.Read(reader);

        return Deserialize<T>(rubyReader.Root, settings);;
    }

    public static T? Deserialize<T>(AbstractEntity data, ReaderSettings? settings = null)
    {
        var instance = new RubyConverter(settings);

        return (T?)SerializationHelper.AssignmentConversion(typeof(T), instance.DeserializeEntity(data), typeof(T) == typeof(object));
    }

    private object DeserializeObject(Type type, RubyObject data)
    {
        var obj = Activator.CreateInstance(type)!;

        foreach (var (key, value) in data.Fields)
        {
            var fieldName = key.ResolveIfLink().ToString()!;

            var candidate = SerializationHelper.Instance.GetFieldCandidate(type, fieldName);
            if (candidate == null /* || data is RubyUserDefined*/)
                StoreToExtensionData(type, obj, fieldName, value);
            else
            {
                ValueWrapper w;

                switch (candidate.Type)
                {
                    case SerializationHelper.CandidateType.Property:
                        w = new() { Object = obj, Property = type.GetProperty(candidate.Name) };
                        break;
                    case SerializationHelper.CandidateType.Field:
                        w = new() { Object = obj, Field = type.GetField(candidate.Name) };
                        break;
                    default:
                        continue;
                }

                w.SetValue(DeserializeEntity(value), candidate.IsDynamic);
            }
        }

        return obj;
    }

    private void StoreToExtensionData(Type type, object obj ,string fieldName, AbstractEntity value)
    {
        var extensionCandidate = SerializationHelper.Instance.GetExtensionDataCandidate(type);
        if (extensionCandidate != null)
        {
            object? extensionData = null;

            switch (extensionCandidate.Type)
            {
                case SerializationHelper.CandidateType.Property:
                    extensionData = type.GetProperty(extensionCandidate.Name)!.GetValue(obj);
                    break;
                case SerializationHelper.CandidateType.Field:
                    extensionData = type.GetField(extensionCandidate.Name)!.GetValue(obj);
                    break;
            }

            if (extensionData != null)
                ((Dictionary<string, AbstractEntity>)extensionData)[fieldName] = value;
        }
        else if (_settings.EnsureExtensionDataPresent)
            throw new Exception($"Ruby object type {type} does not have extension data field");
    }

    private object? DeserializeEntity(AbstractEntity e)
    {
        e = e.ResolveIfLink();

        switch (e.Code)
        {
            case RubyCodes.Symbol:
                return ((RubySymbol)e).ToString();
            case RubyCodes.String:
                return new SpecialString(((RubyString)e).Bytes, Encoding.UTF8);
            case RubyCodes.InstanceVar:
            {
                if (_objectConversionMap.ContainsKey(e))
                    return _objectConversionMap[e];

                var c = DeserializeInstanceVariable((RubyInstanceVariable)e);

                _objectConversionMap[e] = c;

                return c;
            }
            case RubyCodes.Array:
            {
                if (_objectConversionMap.ContainsKey(e))
                    return _objectConversionMap[e];

                IList list = new List<object>();

                _objectConversionMap[e] = list;

                var ra = (RubyArray)e;

                foreach (var t in ra.Elements)
                    list.Add(DeserializeEntity(t));

                return list;

            }
            case RubyCodes.Hash:
            {
                if (_objectConversionMap.ContainsKey(e))
                    return _objectConversionMap[e];

                IDictionary dict = new Dictionary<object, object>();
                
                _objectConversionMap[e] = dict;

                foreach (var re in ((RubyHash)e).Pairs)
                    dict.Add(DeserializeEntity(re.Key), DeserializeEntity(re.Value));

                return dict;
            }
            case RubyCodes.UserDefined:
            {
                if (_objectConversionMap.ContainsKey(e))
                    return _objectConversionMap[e];

                var ru = (RubyUserDefined)e;
                var objectName = ru.GetRealClassName();
                if (!_rubyObjectsClassMap.ContainsKey(objectName))
                    throw new Exception($"Unsupported user-defined object [{objectName}]");

                var c = DeserializeUserDefinedObject(_rubyObjectsClassMap[objectName], ru);

                _objectConversionMap[e] = c;

                return c;
            }
            case RubyCodes.Object:
            {
                if (_objectConversionMap.ContainsKey(e))
                    return _objectConversionMap[e];

                var ro = (RubyObject)e;

                var objectName = ro.GetRealClassName();
                if (!_rubyObjectsClassMap.ContainsKey(objectName))
                    throw new Exception($"Unsupported object [{objectName}]");

                var c = DeserializeObject(_rubyObjectsClassMap[objectName], ro);

                _objectConversionMap[e] = c;

                return c;
            }
            case RubyCodes.True:
                return true;
            case RubyCodes.False:
                return false;
            case RubyCodes.Nil:
                return null;
            case RubyCodes.FixNum:
                return ((RubyFixNum)e).Value;
            case RubyCodes.Float:
                return ((RubyFloat)e).Value;
        }

        throw new Exception($"Unsupported Ruby object [{e.GetType()}]");
    }

    private object DeserializeInstanceVariable(RubyInstanceVariable riv)
    {
        var res = DeserializeEntity(riv.Object)!;
        if (riv.Object.Code == RubyCodes.String)
        {
            if (riv.Variables.Count != 1)
                throw new Exception($"{nameof(RubyString)} instance variable is expected 1 parameter");
            
            // always: E => true
            // external encoding?
        }
        
        if (riv.Object.Code != RubyCodes.String)
        {
            Debug.WriteLine(123);
        }
            
        return res;
    }

    private object DeserializeUserDefinedObject(Type type, RubyUserDefined data)
    {
        var obj = Activator.CreateInstance(type)!;

        if (!_userSerializersByType.ContainsKey(type))
            throw new Exception(
                $"Class [{type}] is used for user-defined ruby object serialization and needs a custom serializer");

        var serializerType = _userSerializersByType[type];
        var serializer = Activator.CreateInstance(serializerType);

        var method = serializerType.GetMethod("Read")!;

        using var stream = new MemoryStream(data.Bytes);
        using var reader = new BinaryReader(stream);
        method.Invoke(serializer, new[] { obj, reader });

        return obj;
    }
}
