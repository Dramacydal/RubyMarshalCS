﻿using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using RubyMarshal.Entities;
using RubyMarshal.Serialization;
using RubyMarshal.Settings;
using RubyMarshal.SpecialTypes;

namespace RubyMarshal;

public class RubyConverter
{
    private readonly ReaderSettings _settings;

    private static Dictionary<string, Type> _rubyObjectsClassMap = new();

    private static Dictionary<Type, Type> _customSerializersByType = new();

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
                RegisterClass((attr as RubyObjectAttribute).Name, t);
            else if (attr.GetType() == typeof(RubyCustomSerializerAttribute))
                RegisterCustomSerializer(t, (attr as RubyCustomSerializerAttribute).Type);
        }
    }

    public static void RegisterClass(string name, Type type)
    {
        if (_rubyObjectsClassMap.ContainsKey(name))
            throw new Exception($"Class [{name}] already registered");

        _rubyObjectsClassMap[name] = type;
    }

    public static void RegisterCustomSerializer(Type type, Type serializer)
    {
        if (_customSerializersByType.ContainsKey(type))
            throw new Exception($"Custom serializer for class [{type}] already registered");

        // TODO: by default read/write bytes ?
        var found = false;
        foreach (var i in serializer.GetInterfaces())
        {
            if (i.IsGenericType)
            {
                var g = i.GetGenericTypeDefinition();
                if (g == typeof(ICustomRubySerializer<>))
                {
                    found = true;
                    break;
                }
            }
        }

        if (!found)
            throw new Exception(
                $"Custom serializer [{serializer}] does not implement ICustomRubySerializer<> interface");

        _customSerializersByType[type] = serializer;
    }

    public static T Deserialize<T>(string path, ReaderSettings? settings = null)
    {
        using var file = File.OpenRead(path);
        using var reader = new BinaryReader(file);

        var r = Deserialize<T>(reader, settings);

        return r;
    }

    public static T Deserialize<T>(BinaryReader reader, ReaderSettings? settings = null)
    {
        var rubyReader = new RubyReader(settings);
        rubyReader.Read(reader);

        var r = Deserialize<T>(rubyReader.Root, settings);
        return r;
    }

    public static T Deserialize<T>(AbstractEntity data, ReaderSettings? settings = null)
    {
        var instance = new RubyConverter(settings);

        return (T)instance.DeserializeEntity(typeof(T), data, typeof(T) == typeof(object));
    }

    private object DeserializeObject(Type type, RubyObject data)
    {
        var obj = Activator.CreateInstance(type);

        foreach (var (key, value) in data.Fields)
        {
            var fieldName = key.ResolveIfLink().ToString();

            var candidate = SerializationHelper.Instance.GetFieldCandidate(type, fieldName);
            if (candidate == null/* || data is RubyUserDefined*/)
            {
                var extensionCandidate = SerializationHelper.Instance.GetExtensionDataCandidate(type);
                if (extensionCandidate != null)
                {
                    object extensionData = null;

                    switch (extensionCandidate.Type)
                    {
                        case SerializationHelper.CandidateType.Property:
                            extensionData = type.GetProperty(extensionCandidate.Name).GetValue(obj);
                            break;
                        case SerializationHelper.CandidateType.Field:
                            extensionData = type.GetField(extensionCandidate.Name).GetValue(obj);
                            break;
                    }

                    if (extensionData != null)
                        (extensionData as Dictionary<string, AbstractEntity>)[fieldName] = value;
                }
            }
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

                if (typeof(AbstractDynamicProperty).IsAssignableFrom(w.Type))
                {
                    var dynamicInstance = Activator.CreateInstance(w.Type) as AbstractDynamicProperty;
                    dynamicInstance.Set(DeserializeEntity(typeof(object), value, true));

                    w.SetValue(dynamicInstance, candidate.IsDynamic);
                }
                else
                    w.SetValue(DeserializeEntity(w.Type, value, candidate.IsDynamic), candidate.IsDynamic);
            }
        }

        return obj;
    }

    private object DeserializeEntity(Type t, AbstractEntity e, bool allowDynamic)
    {
        e = e.ResolveIfLink();

        if (e is RubySymbol rsy)
            return rsy.ToString();

        if (e is RubyString rst)
        {
            if (typeof(IList).IsAssignableFrom(t))
            {
                var list = Activator.CreateInstance(t) as IList;
                foreach (var re in rst.Bytes)
                    list.Add(re);
                return list;
            }

            return new SpecialString(rst.Bytes, Encoding.UTF8);
        }

        if (e is RubyFixNum rfn)
            return rfn.Value;

        if (e is RubyFloat rfl)
            return rfl.Value;

        if (e is RubyInstanceVariable riv)
        {
            if (_objectConversionMap.ContainsKey(e))
                return _objectConversionMap[e];

            var c = DeserializeInstanceVariable(t, riv, allowDynamic);
            
            _objectConversionMap[e] = c;

            return c;
        }

        if (e is RubyTrue)
            return true;

        if (e is RubyFalse)
            return false;

        if (e is RubyNil)
            return null;

        if (e is RubyArray ra)
        {
            if (_objectConversionMap.ContainsKey(e))
                return _objectConversionMap[e];

            IList list;
            Type valueType;
            if (typeof(IList).IsAssignableFrom(t))
            {
                list = Activator.CreateInstance(t) as IList;
                valueType = SerializationHelper.Instance.SearchElementTypeForList(t);
            }
            else if (t == typeof(object) && allowDynamic)
            {
                list = new List<object>();
                valueType = typeof(object);
            }
            else
                throw new Exception("Type does not implement IList");

            _objectConversionMap[e] = list;

            for (var i = 0; i < ra.Elements.Count; ++i)
            {
                var vv = DeserializeEntity(valueType, ra.Elements[i], allowDynamic);

                if (vv != null && valueType != typeof(object) && valueType != vv.GetType())
                    vv = ValueWrapper.ManualCast(valueType, vv);
                
                list.Add(vv);
            }

            return list;
        }

        if (e is RubyHash rh)
        {
            if (_objectConversionMap.ContainsKey(e))
                return _objectConversionMap[e];

            IDictionary dict;
            Type keyType, valueType;
            if (typeof(IDictionary).IsAssignableFrom(t))
            {
                dict = Activator.CreateInstance(t) as IDictionary;
                (keyType, valueType) = SerializationHelper.Instance.SearchElementTypesForDictionary(t);
            }
            else if (t == typeof(object) && allowDynamic)
            {
                dict = new Dictionary<object, object>();
                keyType = valueType = typeof(object);
            }
            else
                throw new Exception("Type does not implement IList");

            _objectConversionMap[e] = dict;

            foreach (var re in rh.Pairs)
                dict.Add(DeserializeEntity(keyType, re.Key, allowDynamic), DeserializeEntity(valueType, re.Value, allowDynamic));

            return dict;
        }

        if (e is RubyUserDefined ru)
        {
            if (_objectConversionMap.ContainsKey(e))
                return _objectConversionMap[e];

            var objectName = ru.GetRealClassName();
            if (!_rubyObjectsClassMap.ContainsKey(objectName))
                throw new Exception($"Unsupported user-defined object [{objectName}]");

            var c = DeserializeUserDefinedObject(_rubyObjectsClassMap[objectName], ru);

            _objectConversionMap[e] = c;

            return c;
        }

        if (e is RubyObject ro)
        {
            if (_objectConversionMap.ContainsKey(e))
                return _objectConversionMap[e];

            var objectName = ro.GetRealClassName();
            if (!_rubyObjectsClassMap.ContainsKey(objectName))
                throw new Exception($"Unsupported object [{objectName}]");

            var c = DeserializeObject(_rubyObjectsClassMap[objectName], ro);

            _objectConversionMap[e] = c;

            return c;
        }

        throw new Exception($"Unsupported [{e.GetType()}]");
    }

    private object DeserializeInstanceVariable(Type type, RubyInstanceVariable riv, bool allowDynamic)
    {
        var res = DeserializeEntity(type, riv.Object, allowDynamic);
        if (riv.Object is RubyString)
        {
            if (riv.Variables.Count != 1)
                throw new Exception($"{nameof(RubyString)} instance variable is expected 1 parameter");
            
            // always: E => true
            // external encoding?
        }
        
        if (riv.Object is not RubyString)
        {
            Debug.WriteLine(123);
        }
            
        return res;
    }

    private object DeserializeUserDefinedObject(Type type, RubyUserDefined data)
    {
        var obj = Activator.CreateInstance(type);

        if (!_customSerializersByType.ContainsKey(type))
            throw new Exception(
                $"Class [{type}] is used for user-defined ruby object serialization and needs a custom serializer");

        var serializerType = _customSerializersByType[type];
        var serializer = Activator.CreateInstance(serializerType);

        var method = serializerType.GetMethod("Read");

        using (var stream = new MemoryStream(data.Bytes))
        using (var reader = new BinaryReader(stream))
            method.Invoke(serializer, new[] { obj, reader });

        return obj;
    }
}
