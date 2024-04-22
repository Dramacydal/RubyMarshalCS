using System.Collections;
using System.Diagnostics;
using System.Text;
using RubyMarshal.Entities;
using RubyMarshal.Enums;
using RubyMarshal.Serialization;
using RubyMarshal.Settings;
using RubyMarshal.SpecialTypes;

namespace RubyMarshal;

public class RubyDeserializer
{
    private readonly ReaderSettings _settings;

    private Dictionary<object, object> _objectConversionMap = new();

    private RubyDeserializer(ReaderSettings? settings = null)
    {
        _settings = settings ?? new();
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
        var instance = new RubyDeserializer(settings);

        return (T?)SerializationHelper.AssignmentConversion(typeof(T), instance.DeserializeEntity(data), typeof(T) == typeof(object));
    }

    private object DeserializeObject(Type type, RubyObject data)
    {
        var obj = Activator.CreateInstance(type)!;

        foreach (var (key, value) in data.Fields)
        {
            var fieldName = key.ResolveIfLink().ToString()!;

            var candidate = SerializationHelper.GetFieldCandidate(type, fieldName);
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
        var extensionCandidate = SerializationHelper.GetExtensionDataCandidate(type);
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

                var objectType = SerializationHelper.GetTypeForRubyObject(objectName);
                if (objectType == null)
                    throw new Exception($"Unsupported user-defined object [{objectName}]");

                var c = DeserializeUserDefinedObject(objectType, ru);

                _objectConversionMap[e] = c;

                return c;
            }
            case RubyCodes.Object:
            {
                if (_objectConversionMap.ContainsKey(e))
                    return _objectConversionMap[e];

                var ro = (RubyObject)e;

                var objectName = ro.GetRealClassName();
                var objectType = SerializationHelper.GetTypeForRubyObject(objectName);
                if (objectType == null)
                    throw new Exception($"Unsupported object [{objectName}]");

                var c = DeserializeObject(objectType, ro);

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

            var v = riv.Variables[0];

            var realVar = riv.Context.LookupSymbol(v.Key);
            
            if (realVar.ToString() == "E")
            {
                if (v.Value is RubyTrue)
                    ((SpecialString)res).Encoding = Encoding.UTF8;
                else if (v.Value is RubyFalse)
                    ((SpecialString)res).Encoding = Encoding.ASCII;
                else
                {
                    // string encoding
                }
            }
            
            // True - UTF-8
            // False - ASCII
            // string - other encoding name
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

        var serializerType = SerializationHelper.GetUserSerializerByType(type);
        if (serializerType == null)
            throw new Exception(
                $"Class [{type}] is used for user-defined ruby object serialization and needs a custom serializer");

        var serializer = Activator.CreateInstance(serializerType);

        var method = serializerType.GetMethod("Read")!;

        using var stream = new MemoryStream(data.Bytes);
        using var reader = new BinaryReader(stream);
        method.Invoke(serializer, new[] { obj, reader });

        return obj;
    }
}
