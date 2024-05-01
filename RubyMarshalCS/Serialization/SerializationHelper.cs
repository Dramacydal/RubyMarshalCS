﻿using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using RubyMarshalCS.Conversion.Attributes;
using RubyMarshalCS.Conversion.Interfaces;
using RubyMarshalCS.Serialization.Attributes;
using RubyMarshalCS.Serialization.Interfaces;
using RubyMarshalCS.SpecialTypes;
using RubyMarshalCS.SpecialTypes.Interfaces;

namespace RubyMarshalCS.Serialization;

public class SerializationHelper
{
    public static bool AutoRegister { get; set; } = true;

    private SerializationHelper()
    {
        Initialize();
    }

    private static SerializationHelper? _instance;

    private static SerializationHelper GetInstance()
    {
        if (_instance == null)
            _instance = new SerializationHelper();

        return _instance;
    }

    private void Initialize()
    {
        if (!AutoRegister)
            return;
        
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        foreach (var t in a.GetTypes())
        foreach (var attr in t.GetCustomAttributes())
        {
            if (attr.GetType() == typeof(RubyObjectAttribute))
            {
                var ro = (RubyObjectAttribute)attr;
                RegisterRubyObject(ro.Name, t, ro.ContextTag);
            }
            else if (attr.GetType() == typeof(RubyUserSerializerAttribute))
            {
                var rus = (RubyUserSerializerAttribute)attr;
                RegisterUserObjectSerializer(t, rus.Type, rus.ContextTag);
            }
            else if (attr.GetType() == typeof(CustomConverterAttribute))
                RegisterCustomConverter(t);
        }
    }

    public static void RegisterRubyObject(Type t)
    {
        foreach (var attr in t.GetCustomAttributes())
        {
            if (attr.GetType() == typeof(RubyObjectAttribute))
            {
                var ro = (RubyObjectAttribute)attr;
                GetInstance().RegisterRubyObject(ro.Name, t, ro.ContextTag);

                return;
            }
        }

        throw new Exception($"Type {t} does not have {nameof(RubyObjectAttribute)} assigned");
    }

    private void RegisterRubyObject(string name, Type type, string tag)
    {
        if (!_rubyObjectTypeNamesToTypeMap.ContainsKey(tag))
        {
            _rubyObjectTypeNamesToTypeMap[tag] = new();
            _typeToRubyObjectMap[tag] = new();
        }

        if (_rubyObjectTypeNamesToTypeMap[tag].ContainsKey(name))
            throw new Exception($"Ruby object [{name}] by tag \"{tag}\" already registered");

        _rubyObjectTypeNamesToTypeMap[tag][name] = type;
        _typeToRubyObjectMap[tag][type] = name;
    }

    public static void RegisterUserObjectSerializer(Type serializer)
    {
        foreach (var attr in serializer.GetCustomAttributes())
        {
            if (attr.GetType() == typeof(RubyUserSerializerAttribute))
            {
                var rus = (RubyUserSerializerAttribute)attr;
                GetInstance().RegisterUserObjectSerializer(serializer, rus.Type, rus.ContextTag);
            }
        }
        
        throw new Exception($"Type {serializer} does not have {nameof(RubyUserSerializerAttribute)} assigned");
    }

    public void RegisterUserObjectSerializer(Type type, Type serializer, string tag)
    {
        if (!_userSerializersByType.ContainsKey(tag))
            _userSerializersByType[tag] = new();

        if (_userSerializersByType[tag].ContainsKey(type))
            throw new Exception($"User object serializer for type [{type}] by tag \"{tag}\" already registered");

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

        _userSerializersByType[tag][type] = serializer;
    }

    private void RegisterCustomConverter(Type type)
    {
        if (!typeof(ICustomConverter).IsAssignableFrom(type))
            throw new Exception($"Type {type} must implement ICustomConverter attribute");

        _customConverters.Add((ICustomConverter)Activator.CreateInstance(type)!);
    }

    public enum CandidateType
    {
        Field,
        Property,
    }

    public class Candidate
    {
        public Candidate(CandidateType type, string name)
        {
            Type = type;
            Name = name;
        }

        public Candidate(CandidateType type, string name, bool isTrash, bool isDynamic)
        {
            Type = type;
            Name = name;
            IsTrash = isTrash;
            IsDynamic = isDynamic;
        }

        public CandidateType Type { get; }
        public string Name { get; }
        public bool IsDynamic { get; }
        public bool IsTrash { get; }
    }

    public class TypeCandidateInfo
    {
        public TypeCandidateInfo(Type type)
        {
            Type = type;
        }

        public Type Type { get; }

        public readonly Dictionary<string, Candidate> FieldCandidates = new();

        public Candidate? ExtensionDataCandidate { get; set; }
    }

    private static Dictionary<Type, TypeCandidateInfo> _infos = new();

    private static Dictionary<Type, Type?> _elementTypeForListMap = new();

    private static Dictionary<Type, Tuple<Type, Type>?> _elemenTypeForDictionaryMap = new();

    private Dictionary<string, Dictionary<string, Type>> _rubyObjectTypeNamesToTypeMap = new();
    private Dictionary<string, Dictionary<Type, string>> _typeToRubyObjectMap = new();

    private Dictionary<string, Dictionary<Type, Type>> _userSerializersByType = new();

    private List<ICustomConverter> _customConverters = new();

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

                info.FieldCandidates[ra.Name] = new(candidateType, name, ra.IsTrash,
                    attributes.Any(_ => _ is RubyDynamicPropertyAttribute));
            }

            if (attr is RubyExtensionDataAttribute re)
            {
                if (type is PropertyInfo pi)
                {
                    if (pi.PropertyType != typeof(Dictionary<string, object?>))
                        throw new Exception(
                            $"{nameof(RubyExtensionDataAttribute)} on wrong property type, must be Dictionary<string, object?>");
                }

                if (type is FieldInfo fi)
                {
                    if (fi.FieldType != typeof(Dictionary<string, object?>))
                        throw new Exception(
                            $"{nameof(RubyExtensionDataAttribute)} on wrong field type, must be Dictionary<string, object?>");
                }

                info.ExtensionDataCandidate = new(candidateType, name);
            }
        }
    }

    public static TypeCandidateInfo GetTypeCandidateInfo(Type type)
    {
        if (_infos.ContainsKey(type))
            return _infos[type];

        TypeCandidateInfo info = new(type);

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

    private static Type SearchElementTypeForList(Type type)
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

    private static Tuple<Type, Type> SearchElementTypesForDictionary(Type type)
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
        if (o.GetType() == type || type == typeof(object))
            return o;

        var converter = GetCustomConverter(o, type);
        if (converter != null)
            return converter.Convert(o, type);

        if (o.GetType() != typeof(SpecialString))
        {
            // throw new Exception("DEBUG");
        }

        var dataParam = Expression.Parameter(typeof(object), "data");
        var body = Expression.Block(Expression.Convert(Expression.Convert(dataParam, o.GetType()), type));

        var run = Expression.Lambda(body, dataParam).Compile();

        return run.DynamicInvoke(o)!;
    }

    private static ICustomConverter? GetCustomConverter(object o, Type type)
    {
        return GetInstance()._customConverters.FirstOrDefault(converter => converter.CanConvert(o, type));
    }

    public static ICustomConverter? GetBackConverter(object o)
    {
        return GetInstance()._customConverters.FirstOrDefault(converter => converter.CanConvertBack(o));
    }

    public static object? AssignmentConversion(Type t, object? o, bool allowDynamic)
    {
        if (o == null)
            return null;

        if (typeof(IDynamicProperty).IsAssignableFrom(t))
        {
            var dynamicInstance = (IDynamicProperty)Activator.CreateInstance(t)!;
            dynamicInstance.Set(o);

            return dynamicInstance;
        }

        return o switch
        {
            IList list => ListAssignmentConversion(t, list, allowDynamic),
            IDictionary dict => DictionaryAssignmentConversion(t, dict, allowDynamic),
            _ => ManualCast(t, o)
        };
    }

    private static object ListAssignmentConversion(Type newType, IList list, bool allowDynamic)
    {
        IList newList;
        Type valueType;
        if (typeof(IList).IsAssignableFrom(newType))
        {
            newList = (IList)Activator.CreateInstance(newType)!;
            valueType = SearchElementTypeForList(newType);
        }
        else if (newType == typeof(object) && allowDynamic)
        {
            newList = new List<object>();
            valueType = typeof(object);
        }
        else
            throw new Exception($"Type {newType} does not implement IList");

        foreach (var e in list)
        {
            if (e != null && valueType != typeof(object) && valueType != e.GetType())
                newList.Add(ManualCast(valueType, e));
            else
                newList.Add(e);
        }

        return newList;
    }

    private static object DictionaryAssignmentConversion(Type newType, IDictionary dict, bool allowDynamic)
    {
        IDictionary newDict;
        Tuple<Type, Type> valueType;
        if (typeof(IDictionary).IsAssignableFrom(newType))
        {
            newDict = (IDictionary)Activator.CreateInstance(newType)!;
            valueType = SearchElementTypesForDictionary(newType);
        }
        else if (newType == typeof(object) && allowDynamic)
        {
            newDict = new Dictionary<object, object>();
            valueType = new(typeof(object), typeof(object));
        }
        else
            throw new Exception($"Type {newType} does not implement IList");

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

    public static Type? GetTypeForRubyObjectTypeName(string rubyObjectTypeName, string tag)
    {
        return GetInstance()._rubyObjectTypeNamesToTypeMap.FirstOrDefault(_ => _.Key == tag).Value?
            .FirstOrDefault(_ => _.Key == rubyObjectTypeName).Value;
    }

    public static string? GetRubyObjectTypeNameForType(Type type, string tag)
    {
        return GetInstance()._rubyObjectTypeNamesToTypeMap.FirstOrDefault(_ => _.Key == tag).Value?.FirstOrDefault(_ => _.Value == type).Key;
    }

    public static Type? GetUserSerializerByType(Type type, string tag)
    {
        return GetInstance()._userSerializersByType.FirstOrDefault(_ => _.Key == tag).Value?.FirstOrDefault(_ => _.Key == type).Value;
    }
}
