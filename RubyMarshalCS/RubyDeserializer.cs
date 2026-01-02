using System.Collections;
using System.Text;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;
using RubyMarshalCS.Exceptions;
using RubyMarshalCS.Serialization;
using RubyMarshalCS.Serialization.Enums;
using RubyMarshalCS.Settings;
using RubyMarshalCS.SpecialTypes;

namespace RubyMarshalCS;

public class RubyDeserializer
{
    private readonly DeserializationSettings _settings;

    private readonly Dictionary<object, object> _objectConversionMap = new();

    public RubyDeserializer(DeserializationSettings? settings = null)
    {
        _settings = settings ?? new();
    }

    public T? Deserialize<T>(AbstractEntity data)
    {
        var customConverter = SerializationHelper.GetCustomConverter(typeof(T));
        if (customConverter != null)
            return (T?)customConverter.Deserialize(data, this);

        return (T?)SerializationHelper.AssignmentConversion(typeof(T), DeserializeEntity(data),
            typeof(T) == typeof(object));
    }

    private object DeserializeObject(Type type, RubyObject data)
    {
        var obj = Activator.CreateInstance(type)!;

        var info = SerializationHelper.GetTypeCandidateInfo(type);

        info.OnDeserializingMethod?.Invoke(obj, [data]);

        foreach (var (key, value) in data.Fields)
        {
            var fieldName = key.ResolveIfLink().ToString()!;

            if (info.FieldCandidates.TryGetValue(fieldName, out var candidate))
            {
                if ((candidate.Flags & CandidateFlags.InOut) != 0 && !candidate.Flags.HasFlag(CandidateFlags.In) &&
                    _settings.ConsiderInOutFields)
                    continue;

                ValueWrapper.SetValue(candidate, obj, DeserializeEntity(value), candidate.Flags.HasFlag(CandidateFlags.Dynamic));
            }
            else
            {
                var handling = _settings.MissingMemberHandling;

                var deserializedValue = DeserializeEntity(value);

                if (handling == MissingMemberHandling.Error)
                {
                    var args = new MissingMemberArgs
                    {
                        RubyObject = data,
                        Object = obj,
                        Member = new(){ Key = fieldName, Value = deserializedValue},
                    };

                    _settings.MissingMember?.Invoke(this, args);

                    handling = args.Action;

                    if (handling == MissingMemberHandling.Error)
                        throw new DeserializationException(
                            $"Object {data.GetRealClassName()} has unmapped property \"{fieldName}\"");
                }

                if (handling == MissingMemberHandling.Store)
                    StoreToExtensionData(info, obj, fieldName, deserializedValue);
            }
        }

        info.OnDeserializedMethod?.Invoke(obj, [data]);

        return obj;
    }

    private void StoreToExtensionData(TypeCandidateInfo extensionCandidate, object obj, string fieldName, object? value)
    {
        if (extensionCandidate.ExtensionDataCandidate != null)
        {
            var extensionData = extensionCandidate.ExtensionDataCandidate.GetValue(obj);
            if (extensionData != null)
                ((Dictionary<string, object?>)extensionData)[fieldName] = value;
        }
        else
            throw new Exception($"Ruby object type {extensionCandidate.Type} does not have extension data field");
    }

    private object? DeserializeEntity(AbstractEntity e)
    {
        e = e.ResolveIfLink();

        switch (e.Code)
        {
            case RubyCodes.Symbol:
            {
                if (_objectConversionMap.TryGetValue(e, out var entity))
                    return entity;

                var value = LookupEncoding(e, SerializationHelper.ASCII8BitEncoding)!.GetString(((RubySymbol)e).Value);

                _objectConversionMap[e] = value;

                return value;
            }
            case RubyCodes.String:
            {
                if (_objectConversionMap.TryGetValue(e, out var entity))
                    return entity;

                var value = new BinaryString(((RubyString)e).Bytes, LookupEncoding(e));

                _objectConversionMap[e] = value;

                return value;
            }
            case RubyCodes.Array:
            {
                if (_objectConversionMap.TryGetValue(e, out var entity))
                    return entity;

                IList list = new List<object>();

                _objectConversionMap[e] = list;

                var ra = (RubyArray)e;

                foreach (var t in ra.Elements)
                    list.Add(DeserializeEntity(t));

                return list;

            }
            case RubyCodes.Hash:
            {
                if (_objectConversionMap.TryGetValue(e, out var entity))
                    return entity;

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
                if (_objectConversionMap.TryGetValue(e, out var entity))
                    return entity;

                var ru = (RubyUserDefined)e;
                var objectName = ru.GetRealClassName();

                var objectType = SerializationHelper.GetTypeForRubyObjectTypeName(objectName, _settings.ContextTag);
                if (objectType == null)
                {
                    var handling = _settings.UndefinedUserObjectHandling;
                    if (handling == UndefinedUserObjectHandling.Error)
                    {
                        var args = new UndefinedUserObjectArgs
                        {
                            RubyUserObject = ru,
                        };
                        _settings.UndefinedUserObject?.Invoke(this, args);
                        handling = args.Action;

                        if (handling == UndefinedUserObjectHandling.Error)
                            throw new Exception($"Unsupported user-defined object [{objectName}]");
                        if (handling == UndefinedUserObjectHandling.Store)
                            objectType = typeof(GenericUserObject);
                        else if (handling == UndefinedUserObjectHandling.Ignore)
                            return null;
                    }
                }

                var c = DeserializeUserDefinedObject(objectName, objectType!, ru);

                _objectConversionMap[e] = c;

                return c;
            }
            case RubyCodes.Object:
            {
                if (_objectConversionMap.TryGetValue(e, out var entity))
                    return entity;

                var ro = (RubyObject)e;

                var objectName = ro.GetRealClassName();
                var objectType = SerializationHelper.GetTypeForRubyObjectTypeName(objectName, _settings.ContextTag);
                if (objectType == null)
                {
                    var handling = _settings.UndefinedObjectHandling; 
                    if (handling == UndefinedObjectHandling.Error)
                    {
                        var args = new UndefinedObjectArgs
                        {
                            RubyObject = ro,
                        };
                        
                        _settings.UndefinedObject?.Invoke(this, args);
                        handling = args.Action;
                    }
                    
                    switch (handling)
                    {
                        case UndefinedObjectHandling.Error:
                            throw new Exception($"Unsupported object [{objectName}]");
                        case UndefinedObjectHandling.Ignore:
                            return null;
                    }
                }

                var c = DeserializeObject(objectType!, ro);

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

    private Encoding? LookupEncoding(AbstractEntity entity, Encoding? defaultEncoding = null)
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

        return defaultEncoding;
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
        method.Invoke(serializer, [obj, reader]);

        if (obj is GenericUserObject guo)
            guo.Name = objectName;

        return obj;
    }
}
