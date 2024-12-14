using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;
using RubyMarshalCS.Serialization;
using RubyMarshalCS.Serialization.Enums;
using RubyMarshalCS.Settings;
using RubyMarshalCS.SpecialTypes;

namespace RubyMarshalCS;

public class RubyDeserializer
{
    private readonly DeserializationSettings _settings;

    private readonly Dictionary<object, object> _objectConversionMap = new();

    private RubyDeserializer(DeserializationSettings? settings = null)
    {
        _settings = settings ?? new();
    }

    public static T? Deserialize<T>(AbstractEntity data, DeserializationSettings? settings = null)
    {
        var instance = new RubyDeserializer(settings);

        return (T?)SerializationHelper.AssignmentConversion(typeof(T), instance.DeserializeEntity(data), typeof(T) == typeof(object));
    }

    private object DeserializeObject(Type type, RubyObject data)
    {
        var obj = Activator.CreateInstance(type)!;

        var info = SerializationHelper.GetTypeCandidateInfo(type);

        info.OnPreDeserializeMethod?.Invoke(obj, new object[] { data });

        foreach (var (key, value) in data.Fields)
        {
            var fieldName = key.ResolveIfLink().ToString()!;

            if (info.FieldCandidates.TryGetValue(fieldName, out var candidate))
            {
                if ((candidate.Flags & CandidateFlags.InOut) != 0 && !candidate.Flags.HasFlag(CandidateFlags.In) &&
                    _settings.ConsiderInOutFields)
                    continue;

                ValueWrapper w;

                switch (candidate.Type)
                {
                    case CandidateType.Property:
                        w = new(obj, (candidate.Member as PropertyInfo)!);
                        break;
                    case CandidateType.Field:
                        w = new(obj, (candidate.Member as FieldInfo)!);
                        break;
                    default:
                        continue;
                }

                w.SetValue(DeserializeEntity(value), candidate.Flags.HasFlag(CandidateFlags.Dynamic));
            }
            else
            {
                if (!_settings.AllowUnmappedFields)
                    throw new Exception($"Object {type} has unmapped property \"{fieldName}\"");

                StoreToExtensionData(info, obj, fieldName, DeserializeEntity(value));
            }
        }

        info.OnDeserializeMethod?.Invoke(obj, new object[] { data });

        return obj;
    }

    private void StoreToExtensionData(TypeCandidateInfo extensionCandidate, object obj, string fieldName, object? value)
    {
        if (extensionCandidate.ExtensionDataCandidate != null)
        {
            object? extensionData = null;

            switch (extensionCandidate.ExtensionDataCandidate.Type)
            {
                case CandidateType.Property:
                    extensionData = (extensionCandidate.ExtensionDataCandidate.Member as PropertyInfo)!.GetValue(obj);
                    break;
                case CandidateType.Field:
                    extensionData = (extensionCandidate.ExtensionDataCandidate.Member as FieldInfo)!.GetValue(obj);
                    break;
            }

            if (extensionData != null)
                ((Dictionary<string, object?>)extensionData)[fieldName] = value;
        }
        else if (_settings.EnsureExtensionFieldPresent)
            throw new Exception($"Ruby object type {extensionCandidate.Type} does not have extension data field");
    }

    private object? DeserializeEntity(AbstractEntity e)
    {
        e = e.ResolveIfLink();

        switch (e.Code)
        {
            case RubyCodes.Symbol:
            {
                if (_objectConversionMap.ContainsKey(e))
                    return _objectConversionMap[e];

                var value = LookupEncoding(e).GetString(((RubySymbol)e).Value);

                _objectConversionMap[e] = value;

                return value;
            }
            case RubyCodes.String:
            {
                if (_objectConversionMap.ContainsKey(e))
                    return _objectConversionMap[e];

                var value = new BinaryString(((RubyString)e).Bytes, LookupEncoding(e));

                _objectConversionMap[e] = value;

                return value;
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

                var hash = (RubyHash)e;

                IDictionary dict;
                if (hash.Default != null)
                {
                    var dd = new DefDictionary<object, object>
                    {
                        DefaultValue = DeserializeEntity(hash.Default)
                    };
                    dict = dd;
                }
                else
                    dict = new Dictionary<object, object>();

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

                var objectType = SerializationHelper.GetTypeForRubyObjectTypeName(objectName, _settings.ContextTag);
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
                var objectType = SerializationHelper.GetTypeForRubyObjectTypeName(objectName, _settings.ContextTag);
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
            case RubyCodes.BigNum:
                return ((RubyBigNum)e).Value;
        }

        throw new Exception($"Unsupported Ruby object [{e.GetType()}]");
    }

    private Encoding LookupEncoding(AbstractEntity entity)
    {
        foreach (var (key, value) in entity.InstanceVariables)
        {
            if (key.ResolveIfLink().ToString() == "E")
            {
                var e = DeserializeEntity(value);
                if (e is bool b)
                    return b ? Encoding.UTF8 : Encoding.ASCII;

                return e?.ToString() switch
                {
                    "UTF-8" => Encoding.UTF8,
                    "US-ASCII" => Encoding.ASCII,
                    "UTF-16LE" => Encoding.GetEncoding("UTF-16LE"),
                    "UTF-16BE" => Encoding.GetEncoding("UTF-16BE"),
                    "ISO-8859-1" => Encoding.Latin1,
                    "ASCII-8BIT" => SerializationHelper.ASCII8BitEncoding,
                    _ => throw new Exception($"Unsupported encoding [{e}]")
                };
            }
        }

        return SerializationHelper.ASCII8BitEncoding;
    }

    private object DeserializeUserDefinedObject(string objectName, Type type, RubyUserDefined data)
    {
        var serializerType = SerializationHelper.GetUserSerializerByType(type, _settings.ContextTag);
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
