using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
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

    public static readonly Encoding ASCII8BitEncoding;

    static SerializationHelper()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ASCII8BitEncoding = Encoding.GetEncoding(1252);
    }

    private SerializationHelper()
    {
        Initialize();
    }

    private static volatile SerializationHelper? _instance;

    private static readonly Lock InstanceLock = new();

    /// <summary>The one instance, made on first use; a call from another thread waits until it is ready.</summary>
    private static SerializationHelper GetInstance()
    {
        var instance = _instance;
        if (instance != null)
            return instance;

        lock (InstanceLock)
            return _instance ??= new SerializationHelper();
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
            else if (attr.GetType() == typeof(RubyCustomConverterAttribute))
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
        lock (_registryLock)
        {
            var names = _rubyObjectTypeNamesToTypeMap.GetOrAdd(tag, _ => new());
            var types = _typeToRubyObjectMap.GetOrAdd(tag, _ => new());

            if (names.ContainsKey(name))
                throw new Exception($"Ruby object [{name}] by tag \"{tag}\" already registered");

            names[name] = type;
            types[type] = name;
        }
    }

    public static void RegisterUserObjectSerializer(Type serializer)
    {
        foreach (var attr in serializer.GetCustomAttributes())
        {
            if (attr.GetType() == typeof(RubyUserSerializerAttribute))
            {
                var rus = (RubyUserSerializerAttribute)attr;
                GetInstance().RegisterUserObjectSerializer(rus.Type, serializer, rus.ContextTag);
            }
        }

        throw new Exception($"Type {serializer} does not have {nameof(RubyUserSerializerAttribute)} assigned");
    }

    public void RegisterUserObjectSerializer(Type type, Type serializer, string tag)
    {
        lock (_registryLock)
        {
            var serializers = _userSerializersByType.GetOrAdd(tag, _ => new());
            if (serializers.ContainsKey(type))
                throw new Exception($"User object serializer for type [{type}] by tag \"{tag}\" already registered");

            CheckUserSerializer(serializer);

            serializers[type] = serializer;
        }
    }

    private static void CheckUserSerializer(Type serializer)
    {
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
    }

    private void RegisterCustomConverter(Type type)
    {
        if (!typeof(IRubyCustomConverter).IsAssignableFrom(type))
            throw new Exception($"Type {type} must implement ICustomConverter attribute");

        RegisterCustomConverter((IRubyCustomConverter)Activator.CreateInstance(type)!);
    }

    private void RegisterCustomConverter(IRubyCustomConverter converter)
    {
        lock (_registryLock)
            _customConverters = [.._customConverters, converter];
    }

    // Everything below is read by any number of threads that deserialize or serialize at the same time. The caches only grow
    // (an entry is made once and never changes); the registries are written under _registryLock and read without it.

    // a Lazy makes sure that the info of a type is built once, not by every thread that asks for it at the same moment
    private static readonly ConcurrentDictionary<Type, Lazy<TypeCandidateInfo>> _infos = new();

    private static readonly ConcurrentDictionary<Type, Type?> _elementTypeForListMap = new();

    private static readonly ConcurrentDictionary<Type, Tuple<Type, Type>?> _elemenTypeForDictionaryMap = new();

    private readonly Lock _registryLock = new();

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Type>> _rubyObjectTypeNamesToTypeMap = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, string>> _typeToRubyObjectMap = new();

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, Type>> _userSerializersByType = new();

    // replaced as a whole when a converter is added, so a reader goes through a list that does not change
    private volatile IRubyCustomConverter[] _customConverters = [];

    public static Candidate? GetFieldCandidate(Type type, string fieldName)
    {
        return GetTypeCandidateInfo(type).FieldCandidates.GetValueOrDefault(fieldName);
    }

    private static void AttributeChecker(MemberInfo member, TypeCandidateInfo info)
    {
        var attributes = member.GetCustomAttributes(true);
        foreach (var attr in attributes)
        {
            if (attr is RubyPropertyAttribute ra)
            {
                var propertyName = !string.IsNullOrWhiteSpace(ra.PropertyName) ? ra.PropertyName : member.Name;
                if (info.FieldCandidates.ContainsKey(propertyName))
                    throw new Exception(
                        $"Type [{member.DeclaringType.Name}] has duplicate property [{propertyName}]");

                var converterAttr = member.GetCustomAttribute<RubyPropertyConverterAttribute>();
                info.FieldCandidates[propertyName] = new(member, ra.Flags)
                {
                    Converter = converterAttr != null
                        ? (IRubyPropertyConverter)Activator.CreateInstance(converterAttr.ConverterType)!
                        : null
                };
            }

            if (attr is RubyExtensionDataAttribute re)
            {
                if (member is PropertyInfo pi)
                {
                    if (pi.PropertyType != typeof(Dictionary<string, object?>))
                        throw new Exception(
                            $"{nameof(RubyExtensionDataAttribute)} on wrong property type, must be Dictionary<string, object?>");
                }

                if (member is FieldInfo fi)
                {
                    if (fi.FieldType != typeof(Dictionary<string, object?>))
                        throw new Exception(
                            $"{nameof(RubyExtensionDataAttribute)} on wrong field type, must be Dictionary<string, object?>");
                }

                info.ExtensionDataCandidate = new(member);
            }

            if (attr is RubyOnDeserializingAttribute)
                info.OnDeserializingMethod = member as MethodInfo;
            
            if (attr is RubyOnDeserializedAttribute)
                info.OnDeserializedMethod = member as MethodInfo;
            
            if (attr is RubyOnSerializingAttribute)
                info.OnSerializingMethod = member as MethodInfo;
            
            if (attr is RubyOnSerializedAttribute)
                info.OnSerializedMethod = member as MethodInfo;
        }
    }

    public static TypeCandidateInfo GetTypeCandidateInfo(Type type)
    {
        return _infos.GetOrAdd(type, t => new Lazy<TypeCandidateInfo>(() => BuildTypeCandidateInfo(t))).Value;
    }

    private static TypeCandidateInfo BuildTypeCandidateInfo(Type type)
    {
        TypeCandidateInfo info = new(type);

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            AttributeChecker(field, info);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            AttributeChecker(property, info);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            AttributeChecker(method, info);

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

        var result = _elementTypeForListMap.GetOrAdd(type, FindElementTypeForList);
        if (result != null)
            return result;

        throw new Exception($"Failed to determine type of generic list [{type}]");
    }

    // the type itself, then its base types, until one of them is a generic list
    private static Type? FindElementTypeForList(Type? type)
    {
        for (; type != null; type = type.BaseType)
        {
            var g = SearchTypeForGenericList(type);
            if (g != null)
                return g;
        }

        return null;
    }

    private static Tuple<Type, Type> SearchElementTypesForDictionary(Type type)
    {
        if (!typeof(IDictionary).IsAssignableFrom(type))
            throw new Exception($"Type [{type}] does not implement IDictionary");

        var result = _elemenTypeForDictionaryMap.GetOrAdd(type, FindElementTypesForDictionary);
        if (result != null)
            return result;

        throw new Exception($"Failed to determine type of generic dictionary [{type}]");
    }

    // the type itself, then its base types, until one of them is a generic dictionary
    private static Tuple<Type, Type>? FindElementTypesForDictionary(Type? type)
    {
        for (; type != null; type = type.BaseType)
        {
            var g = SearchTypesForGenericDictionary(type);
            if (g != null)
                return g;
        }

        return null;
    }

    private static object ManualCast(Type type, object o)
    {
        var objectType = o.GetType();
        
        if (objectType == type || type == typeof(object))
            return o;

        if (objectType.IsPrimitive && type.IsPrimitive)
            return o;

        if (type.IsEnum && objectType.IsPrimitive)
            return Enum.ToObject(type, Convert.ToInt32(o));

        if (objectType == typeof(BinaryString) && type == typeof(string))
            return ((BinaryString)o).Value;
        
        if (objectType == typeof(string))
        {
            if (type == typeof(BinaryString))
                return new BinaryString((string)o);
            if (type == typeof(bool))
                return Convert.ToBoolean(o);
            if (type == typeof(byte))
                return Convert.ToByte(o);
            if (type == typeof(sbyte))
                return Convert.ToSByte(o);
            if (type == typeof(ushort))
                return Convert.ToUInt16(o);
            if (type == typeof(short))
                return Convert.ToInt16(o);
            if (type == typeof(uint))
                return Convert.ToUInt32(o);
            if (type == typeof(int))
                return Convert.ToInt32(o);
            if (type == typeof(ulong))
                return Convert.ToUInt64(o);
            if (type == typeof(long))
                return Convert.ToInt64(o);
            if (type == typeof(float))
                return Convert.ToSingle(o);
            if (type == typeof(double))
                return Convert.ToDouble(o);
            if (type == typeof(decimal))
                return Convert.ToDecimal(o);
            if (type == typeof(DateTime))
                return Convert.ToDateTime(o);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (Nullable.GetUnderlyingType(type) == o.GetType())
                return o;
        }

        if (type.IsInstanceOfType(o))
            return o;
        
        var dataParam = Expression.Parameter(typeof(object), "data");
        var body = Expression.Block(Expression.Convert(Expression.Convert(dataParam, o.GetType()), type));

        var run = Expression.Lambda(body, dataParam).Compile();

        return run.DynamicInvoke(o)!;
    }

    public static IRubyCustomConverter? GetCustomConverter(Type type)
    {
        return GetInstance()._customConverters.FirstOrDefault(converter => converter.CanConvert(type));
    }

    public static object? AssignmentConversion(Type t, object? o, bool allowDynamic)
    {
        var converter = GetCustomConverter(t);
        if (converter != null)
            return converter.Cast(o);

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
            throw new Exception($"Type {newType} does not implement {nameof(IList)}");

        foreach (var e in list)
        {
            if (e != null && valueType != typeof(object) && valueType != e.GetType())
                newList.Add(AssignmentConversion(valueType, e, allowDynamic));
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
            throw new Exception($"Type {newType} does not implement {nameof(IDictionary)}");

        if (typeof(IDefDictionary).IsAssignableFrom(newType) ^ dict is IDefDictionary)
        {
            throw new Exception($"Both must be or must not be {nameof(IDefDictionary)}");
        }

        foreach (DictionaryEntry e in dict)
        {
            object key, value;
            if (e.Key != null)
                key = ManualCast(valueType.Item1, e.Key);
            else
                key = e.Key;

            if (e.Value != null)
                value = ManualCast(valueType.Item2, e.Value);
            else
                value = e.Value;

            newDict.Add(key, value);
        }

        if (dict is IDefDictionary dd)
        {
            ((IDefDictionary)newDict).DefaultValue =
                dd.DefaultValue != null ? ManualCast(valueType.Item2, dd.DefaultValue) : null;
        }

        return newDict;
    }

    public static Type? GetTypeForRubyObjectTypeName(string rubyObjectTypeName, string tag)
    {
        return GetInstance()._rubyObjectTypeNamesToTypeMap.TryGetValue(tag, out var names) &&
               names.TryGetValue(rubyObjectTypeName, out var type)
            ? type
            : null;
    }

    public static string? GetRubyObjectTypeNameForType(Type type, string tag)
    {
        return GetInstance()._typeToRubyObjectMap.TryGetValue(tag, out var names) && names.TryGetValue(type, out var name)
            ? name
            : null;
    }

    public static Type? GetUserSerializerByType(Type type, string tag)
    {
        return GetInstance()._userSerializersByType.TryGetValue(tag, out var serializers) &&
               serializers.TryGetValue(type, out var serializer)
            ? serializer
            : null;
    }

    /// <summary>The Ruby objects registered by the tag, name to type, as they are now (a copy); null when there are none.</summary>
    public static Dictionary<string, Type>? GetRegisteredRubyObjects(string tag) =>
        GetInstance()._rubyObjectTypeNamesToTypeMap.TryGetValue(tag, out var names) ? new Dictionary<string, Type>(names) : null;
}
