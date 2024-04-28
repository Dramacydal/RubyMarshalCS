using System.Collections;
using System.Diagnostics;
using System.Text;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;
using RubyMarshalCS.Serialization;
using RubyMarshalCS.Settings;
using RubyMarshalCS.SpecialTypes;

namespace RubyMarshalCS;

public class RubyDeserializer
{
    private readonly SerializationSettings _settings;

    private readonly Dictionary<object, object> _objectConversionMap = new();

    private RubyDeserializer(SerializationSettings? settings = null)
    {
        _settings = settings ?? new();
    }

    public static T? Deserialize<T>(AbstractEntity data, SerializationSettings? settings = null)
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
                StoreToExtensionData(type, obj, fieldName, DeserializeEntity(value));
            else
            {
                ValueWrapper w;

                switch (candidate.Type)
                {
                    case SerializationHelper.CandidateType.Property:
                        w = new(obj, type.GetProperty(candidate.Name)!);
                        break;
                    case SerializationHelper.CandidateType.Field:
                        w = new(obj, type.GetField(candidate.Name)!);
                        break;
                    default:
                        continue;
                }

                w.SetValue(DeserializeEntity(value), candidate.IsDynamic);
            }
        }

        return obj;
    }

    private void StoreToExtensionData(Type type, object obj, string fieldName, object? value)
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
                ((Dictionary<string, object?>)extensionData)[fieldName] = value;
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

                var objectType = SerializationHelper.GetTypeForRubyObjectTypeName(objectName);
                if (objectType == null)
                    if (_settings.AllowGenericUserObjects)
                        objectType = typeof(GenericUserObject);
                    else
                        throw new Exception($"Unsupported user-defined object [{objectName}]");

                var c = DeserializeUserDefinedObject(objectName, objectType, ru);

                _objectConversionMap[e] = c;

                return c;
            }
            case RubyCodes.Object:
            {
                if (_objectConversionMap.ContainsKey(e))
                    return _objectConversionMap[e];

                var ro = (RubyObject)e;

                var objectName = ro.GetRealClassName();
                var objectType = SerializationHelper.GetTypeForRubyObjectTypeName(objectName);
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

    private object DeserializeUserDefinedObject(string objectName, Type type, RubyUserDefined data)
    {
        var serializerType = SerializationHelper.GetUserSerializerByType(type);
        if (serializerType == null)
            throw new Exception(
                $"Class [{type}] is used for user-defined ruby object serialization and needs a custom serializer");

        var serializer = Activator.CreateInstance(serializerType);

        var method = serializerType.GetMethod("Read")!;

        using var stream = new MemoryStream(data.Bytes);
        using var reader = new BinaryReader(stream);
        var obj = Activator.CreateInstance(type)!;
        method.Invoke(serializer, new[] { obj, reader });

        if (obj is GenericUserObject guo)
            guo.Name = objectName;

        return obj;
    }
}
