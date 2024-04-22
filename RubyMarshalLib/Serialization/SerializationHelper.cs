using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using RubyMarshal.Entities;
using RubyMarshal.SpecialTypes;

namespace RubyMarshal.Serialization;

public static class SerializationHelper
{
    static SerializationHelper()
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

    public static void RegisterRubyObject(string name, Type type)
    {
        if (_rubyObjectToTypeMap.ContainsKey(name))
            throw new Exception($"Ruby object [{name}] already registered");

        _rubyObjectToTypeMap[name] = type;
        _typeToRubyObjectMap[type] = name;
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

    private static void RegisterCustomConverter(Type type)
    {
        _customConverters.Add((ICustomConverter)Activator.CreateInstance(type)!);
    }

    public enum CandidateType
    {
        Field,
        Property,
    }

    public class Candidate
    {
        public CandidateType Type { get; set; }
        public string Name { get; set; }
        public bool IsDynamic { get; set; }
    }

    public class TypeCandidateInfo
    {
        public Type Type { get; set; }

        public Dictionary<string, Candidate> FieldCandidates = new();
        public Candidate? ExtensionDataCandidate { get; set; }
    }

    private static Dictionary<Type, TypeCandidateInfo> _infos = new();

    private static Dictionary<Type, Type?> _elementTypeForListMap = new();

    private static Dictionary<Type, Tuple<Type, Type>?> _elemenTypeForDictionaryMap = new();

    private static Dictionary<string, Type> _rubyObjectToTypeMap = new();
    private static Dictionary<Type, string> _typeToRubyObjectMap = new();

    private static Dictionary<Type, Type> _userSerializersByType = new();

    private static List<ICustomConverter> _customConverters = new();

    public static Candidate? GetFieldCandidate(Type type, string fieldName)
    {
        var info = GetTypeCandidateInfo(type);

        if (info.FieldCandidates.ContainsKey(fieldName))
            return info.FieldCandidates[fieldName];

        // var strippedName = fieldName.StartsWith('@') ? fieldName.Substring(1) : fieldName;
        //
        // var prop = type.GetProperty(strippedName);
        // if (prop != null)
        //     return new()
        //     {
        //         Type = CandidateType.Property,
        //         Name = prop.Name
        //     };
        //
        // var field = type.GetField(strippedName);
        // if (field != null)
        //     return new()
        //     {
        //         Type = CandidateType.Field,
        //         Name = field.Name
        //     };

        return null;
    }

    public static Candidate? GetExtensionDataCandidate(Type type)
    {
        var info = GetTypeCandidateInfo(type);
        return info?.ExtensionDataCandidate;
    }

    public static bool IsDynamicFieldOrProperty(Type type, string name)
    {
        var info = GetTypeCandidateInfo(type);

        if (!info.FieldCandidates.ContainsKey(name))
            return false;

        var c = info.FieldCandidates[name];

        return c.IsDynamic;
    }

    private static void AttributeChecker(CandidateType candidateType, string name, MemberInfo type,
        TypeCandidateInfo info)
    {
        var attributes = type.GetCustomAttributes(true);
        foreach (var attr in attributes)
        {
            if (attr is RubyPropertyAttribute ra)
            {
                if (info.FieldCandidates.ContainsKey(ra.Name))
                    throw new Exception(
                        $"Type [{type.DeclaringType.Name}] already have field with attribute [{ra.Name}]");

                info.FieldCandidates[ra.Name] = new()
                {
                    Type = candidateType,
                    Name = name,
                    IsDynamic = attributes.Any(_ => _ is RubyDynamicPropertyAttribute)
                };
            }

            if (attr is RubyExtensionDataAttribute re)
            {
                if (type is PropertyInfo pi)
                {
                    if (pi.PropertyType != typeof(Dictionary<string, AbstractEntity>))
                        throw new Exception(
                            "RubyExtensionAttribute on wrong property type, must be Dictionary<string, AbstractEntity>");
                }

                if (type is FieldInfo fi)
                {
                    if (fi.FieldType != typeof(Dictionary<string, AbstractEntity>))
                        throw new Exception(
                            "RubyExtensionAttribute on wrong field type, must be Dictionary<string, AbstractEntity>");
                }

                info.ExtensionDataCandidate = new()
                {
                    Type = candidateType,
                    Name = name,
                };
            }
        }
    }

    public static TypeCandidateInfo GetTypeCandidateInfo(Type type)
    {
        if (_infos.ContainsKey(type))
            return _infos[type];

        TypeCandidateInfo info = new()
        {
            Type = type
        };

        foreach (var field in type.GetFields())
            AttributeChecker(CandidateType.Field, field.Name, field, info);

        foreach (var property in type.GetProperties())
            AttributeChecker(CandidateType.Field, property.Name, property, info);

        _infos[type] = info;

        return info;
    }

    private static Type? SearchTypeForGenericList(Type t)
    {
        foreach (var i in t.GetInterfaces())
        {
            if (i.IsGenericType)
            {
                var g = i.GetGenericTypeDefinition();
                if (g == typeof(IList<>))
                    return i.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static Tuple<Type, Type>? SearchTypesForGenericDictionary(Type t)
    {
        foreach (var i in t.GetInterfaces())
        {
            if (i.IsGenericType)
            {
                var g = i.GetGenericTypeDefinition();
                if (g == typeof(IDictionary<,>))
                {
                    var genericArguments = i.GetGenericArguments();

                    return new(genericArguments[0], genericArguments[1]);
                }
            }
        }

        return null;
    }

    public static Type SearchElementTypeForList(Type type)
    {
        if (!typeof(IList).IsAssignableFrom(type))
            throw new Exception($"Type [{type}] does not implement IList");

        Type? result = null;
        if (!_elementTypeForListMap.ContainsKey(type))
        {
            for (;;)
            {
                var g = SearchTypeForGenericList(type);
                if (g != null)
                {
                    result = g;
                    break;
                }

                type = type.BaseType;
                if (type == null)
                    break;
            }

            _elementTypeForListMap[type!] = result;
        }
        else
            result = _elementTypeForListMap[type];

        if (result != null)
            return result;

        throw new Exception($"Failed to determine type of generic list [{type}]");
    }

    public static Tuple<Type, Type> SearchElementTypesForDictionary(Type type)
    {
        if (!typeof(IDictionary).IsAssignableFrom(type))
            throw new Exception($"Type [{type}] does not implement IDictionary");

        Tuple<Type, Type>? result = null;
        if (!_elemenTypeForDictionaryMap.ContainsKey(type))
        {
            for (;;)
            {
                var g = SearchTypesForGenericDictionary(type);
                if (g != null)
                {
                    result = g;
                    break;
                }

                type = type.BaseType;
                if (type == null)
                    break;
            }

            _elemenTypeForDictionaryMap[type!] = result;
        }
        else
            result = _elemenTypeForDictionaryMap[type];

        if (result != null)
            return result;

        throw new Exception($"Failed to determine type of generic dictionary [{type}]");
    }

    private static object ManualCast(Type type, object o)
    {
        if (o == null)
            return null;

        if (o.GetType() == type)
            return o;

        var converter = GetCustomConverter(o, type);
        if (converter != null)
            return converter.Convert(o, type);

        if (o.GetType() != typeof(SpecialString))
        {
            Debug.WriteLine("");
        }

        var dataParam = Expression.Parameter(typeof(object), "data");
        var body = Expression.Block(Expression.Convert(Expression.Convert(dataParam, o.GetType()), type));

        var run = Expression.Lambda(body, dataParam).Compile();

        return run.DynamicInvoke(o);
    }

    private static ICustomConverter? GetCustomConverter(object o, Type type)
    {
        foreach (var converter in _customConverters)
            if (converter.CanConvert(o, type))
                return converter;

        return null;
    }

    public static object? AssignmentConversion(Type t, object o, bool allowDynamic)
    {
        if (o == null)
            return null;

        if (typeof(AbstractDynamicProperty).IsAssignableFrom(t))
        {
            var dynamicInstance = (AbstractDynamicProperty)Activator.CreateInstance(t);
            dynamicInstance.Set(o);

            return dynamicInstance;
        }

        if (typeof(IList).IsAssignableFrom(o.GetType()))
            return ListAssignmentConversion(t, (IList)o, allowDynamic);
        if (typeof(IDictionary).IsAssignableFrom(o.GetType()))
            return DictionaryAssignmentConversion(t, (IDictionary)o, allowDynamic);

        return ManualCast(t, o);
    }

    private static object? ListAssignmentConversion(Type newType, IList list, bool allowDynamic)
    {
        IList newList;
        Type valueType;
        if (typeof(IList).IsAssignableFrom(newType))
        {
            newList = (IList)Activator.CreateInstance(newType);
            valueType = SearchElementTypeForList(newType);
        }
        else if (newType == typeof(object) && allowDynamic)
        {
            newList = new List<object>();
            valueType = typeof(object);
        }
        else
            throw new Exception("Type {newType} does not implement IList");

        foreach (var e in list)
        {
            if (e != null && valueType != typeof(object) && valueType != e.GetType())
                newList.Add(ManualCast(valueType, e));
            else
                newList.Add(e);
        }

        return newList;
    }

    private static object? DictionaryAssignmentConversion(Type newType, IDictionary dict, bool allowDynamic)
    {
        IDictionary newDict;
        Tuple<Type, Type> valueType;
        if (typeof(IDictionary).IsAssignableFrom(newType))
        {
            newDict = (IDictionary)Activator.CreateInstance(newType);
            valueType = SearchElementTypesForDictionary(newType);
        }
        else if (newType == typeof(object) && allowDynamic)
        {
            newDict = new Dictionary<object, object>();
            valueType = new(typeof(object), typeof(object));
        }
        else
            throw new Exception("Type {newType} does not implement IList");

        foreach (DictionaryEntry e in dict)
        {
            object key, value;
            if (e.Key != null && valueType.Item1 != typeof(object) && valueType.Item1 != e.Key.GetType())
                key = ManualCast(valueType.Item1, e.Key);
            else
                key = e.Key;

            if (e.Value != null && valueType.Item2 != typeof(object) && valueType.Item2 != e.Value.GetType())
                value = ManualCast(valueType.Item2, e.Value);
            else
                value = e.Value;

            newDict.Add(key, value);
        }

        return newDict;
    }

    public static Type GetTypeForRubyObject(string objectName) =>
        _rubyObjectToTypeMap.FirstOrDefault(_ => _.Key == objectName).Value;

    public static string GetRubyObjectForType(Type type) =>
        _rubyObjectToTypeMap.FirstOrDefault(_ => _.Value == type).Key;

    public static Type GetUserSerializerByType(Type type) =>
        _userSerializersByType.FirstOrDefault(_ => _.Key == type).Value;
}
