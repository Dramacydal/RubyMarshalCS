using System.Collections;
using System.Reflection;
using RubyMarshal.Entities;

namespace RubyMarshal.Serialization;

public class SerializationHelper
{
    public static SerializationHelper Instance { get; } = new();

    private SerializationHelper()
    {
        
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
    }

    public class TypeCandidateInfo
    {
        public Type Type { get; set; }

        public Dictionary<string, Candidate> FieldCandidates = new();
        public Candidate ExtensionDataCandidate { get; set; }
    }

    private Dictionary<Type, TypeCandidateInfo> _infos = new();

    private Dictionary<Type, Type?> _elementTypeForListMap = new();

    private Dictionary<Type, Tuple<Type, Type>?> _elemenTypeForDictionaryMap = new();

    public Candidate? GetFieldCandidate(Type type, string fieldName)
    {
        var info = GetTypeCandidateInfo(type);

        if (info.FieldCandidates.ContainsKey(fieldName))
            return info.FieldCandidates[fieldName];

        var strippedName = fieldName.StartsWith('@') ? fieldName.Substring(1) : fieldName;

        var prop = type.GetProperty(strippedName);
        if (prop != null)
            return new()
            {
                Type = CandidateType.Property,
                Name = prop.Name
            };

        var field = type.GetField(strippedName);
        if (field != null)
            return new()
            {
                Type = CandidateType.Field,
                Name = field.Name
            };

        return null;
    }

    public Candidate? GetExtensionDataCandidate(Type type)
    {
        var info = GetTypeCandidateInfo(type);
        return info?.ExtensionDataCandidate;
    }

    private void AttributeChecker(CandidateType candidateType, string name, MemberInfo type, TypeCandidateInfo info)
    {
        foreach (var attr in type.GetCustomAttributes(true))
        {
            if (attr is RubyPropertyAttribute ra)
            {
                if (info.FieldCandidates.ContainsKey(ra.Name))
                    throw new Exception(
                        $"Type [{type.DeclaringType.Name}] already have field with attribute [{ra.Name}]");
                        
                info.FieldCandidates[ra.Name] = new()
                {
                    Type = candidateType,
                    Name = name
                };
            }

            if (attr is RubyExtensionDataAttribute re)
            {
                if (type is PropertyInfo pi)
                {
                    if (pi.PropertyType != typeof(Dictionary<string, AbstractEntity>))
                        throw new Exception("RubyExtensionAttribute on wrong property type");
                }
                
                if (type is FieldInfo fi)
                {
                    if (fi.FieldType != typeof(Dictionary<string, AbstractEntity>))
                        throw new Exception("RubyExtensionAttribute on wrong field type");
                }

                info.ExtensionDataCandidate = new()
                {
                    Type = candidateType,
                    Name = name,
                };
            }
        }

        var attr2 = type.GetCustomAttribute<RubyExtensionDataAttribute>();
        if (attr2 != null)
        {
            
        }
    }

    private TypeCandidateInfo GetTypeCandidateInfo(Type type)
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

    private Type? SearchTypeForGenericList(Type t)
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

    private Tuple<Type, Type>? SearchTypesForGenericDictionary(Type t)
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

    public Type SearchElementTypeForList(Type type)
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

    public Tuple<Type, Type> SearchElementTypesForDictionary(Type type)
    {
        if (!typeof(IDictionary).IsAssignableFrom(type))
            throw new Exception($"Type [{type}] does not implement IDictionary");

        Tuple<Type,Type>? result = null;
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
}
