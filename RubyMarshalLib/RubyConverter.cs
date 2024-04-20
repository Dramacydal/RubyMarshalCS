using System.Collections;
using System.Reflection;
using System.Text;
using RubyMarshal.Entities;
using RubyMarshal.Serialization;

namespace RubyMarshal;

public static class RubyConverter
{
    private static Dictionary<string, Type> _classMap = new();

    private static Dictionary<Type, Type> _customSerializersByType = new();

    static RubyConverter()
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        foreach (var t in a.GetTypes())
        foreach (var attr in t.GetCustomAttributes())
        {
            if (attr.GetType() == typeof(RubyObjectAttribute))
            {
                RegisterClass((attr as RubyObjectAttribute).Name, t);
                continue;
            }

            if (attr.GetType() == typeof(RubyCustomSerializerAttribute))
            {
                RegisterCustomSerializer(t, (attr as RubyCustomSerializerAttribute).Type);
                continue;
            }
        }
    }

    public static void RegisterClass(string name, Type type)
    {
        if (_classMap.ContainsKey(name))
            throw new Exception($"Class [{name}] already registered");

        _classMap[name] = type;
    }

    public static void RegisterCustomSerializer(Type type, Type serializer)
    {
        if (_customSerializersByType.ContainsKey(type))
            throw new Exception($"Custom serializer for class [{type}] already registered");

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

    class ValueWrapper
    {
        public object Object { get; set; }
        public PropertyInfo? Property { get; set; }
        public FieldInfo? Field { get; set; }

        public Type Type => Property?.PropertyType ?? Field?.FieldType;

        public void SetValue(object Value)
        {
            if (Value is IList list)
            {
                AddListElements(list);
                return;
            }

            if (Value is IDictionary dictionary)
            {
                AddDictionaryElements(dictionary);
                return;
            }

            Property?.SetValue(Object, Value);
            Field?.SetValue(Object, Value);
        }

        private void AddListElements(IList values)
        {
            if (Property != null)
            {
                if (typeof(IList).IsAssignableFrom(Property.PropertyType))
                {
                    var list = Property.GetValue(Object) as IList;
                    if (list == null)
                    {
                        list = Activator.CreateInstance(Property.PropertyType) as IList;
                        Property.SetValue(Object, list);
                    }

                    foreach (var e in values)
                        list?.Add(e);
                }
                else
                    throw new Exception($"Property [{Property.Name}] does not implement IList interface");
            }

            if (Field != null)
            {
                if (typeof(IList).IsAssignableFrom(Field.FieldType))
                {
                    var list = Field.GetValue(Object) as IList;
                    if (list == null)
                    {
                        list = Activator.CreateInstance(Field.FieldType) as IList;
                        Field.SetValue(Object, list);
                    }

                    foreach (var e in values)
                        list?.Add(e);
                }
                else
                    throw new Exception($"Field [{Field.Name}] does not implement IList interface");
            }
        }

        private void AddDictionaryElements(IDictionary values)
        {
            if (Property != null)
            {
                if (typeof(IDictionary).IsAssignableFrom(Property.PropertyType))
                {
                    var dictionary = Property.GetValue(Object) as IDictionary;
                    if (dictionary == null)
                    {
                        dictionary = Activator.CreateInstance(Property.PropertyType) as IDictionary;
                        Property.SetValue(Object, dictionary);
                    }

                    foreach (DictionaryEntry e in values)
                        dictionary?.Add(e.Key, e.Value);
                }
                else
                    throw new Exception($"Property [{Property.Name}] does not implement IDictionary interface");
            }

            if (Field != null)
            {
                if (typeof(IDictionary).IsAssignableFrom(Field.FieldType))
                {
                    var dictionary = Field.GetValue(Object) as IDictionary;
                    if (dictionary == null)
                    {
                        dictionary = Activator.CreateInstance(Field.FieldType) as IDictionary;
                        Field.SetValue(Object, dictionary);
                    }

                    foreach (DictionaryEntry e in values)
                        dictionary?.Add(e.Key, e.Value);
                }
                else
                    throw new Exception($"Field [{Field.Name}] does not implement IDictionary interface");
            }
        }
    }

    public static T Deserialize<T>(string path)
    {
        using var file = File.OpenRead(path);
        using var reader = new BinaryReader(file);

        var r = Deserialize<T>(reader);

        return r;
    }

    public static T Deserialize<T>(BinaryReader reader)
    {
        var rubyReader = new RubyReader();
        rubyReader.Read(reader);

        var r = Deserialize<T>(rubyReader.Root);
        return r;
    }

    public static T Deserialize<T>(AbstractEntity data)
    {
        return (T)InitFromObject(typeof(T), data);
    }

    private static object DeserializeObject(Type type, RubyObject data)
    {
        var obj = Activator.CreateInstance(type);

        foreach (var (key, value) in data.Fields)
        {
            var fieldName = key.ResolveIfLink().ToString();

            var candidate = SerializationHelper.Instance.GetFieldCandidate(type, fieldName);
            if (candidate == null || data is RubyUserDefined)
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

                w.SetValue(InitFromObject(w.Type, value));
            }
        }

        return obj;
    }

    private static object InitFromObject(Type t, AbstractEntity e)
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

            if (t == typeof(object))
                return rst.Bytes;

            return Encoding.UTF8.GetString(rst.Bytes);
        }

        if (e is RubyFixNum rfn)
            return rfn.Value;

        if (e is RubyFloat rfl)
            return rfl.Value;

        // TODO: args
        if (e is RubyInstanceVariable riv)
            return InitFromObject(t, riv.Object);

        if (e is RubyTrue)
            return true;

        if (e is RubyFalse)
            return false;

        if (e is RubyNil)
            return null;

        if (e is RubyArray ra)
        {
            if (!typeof(IList).IsAssignableFrom(t))
                throw new Exception("Type does not implement IList");

            var list = Activator.CreateInstance(t) as IList;

            foreach (var re in ra.Elements)
                list.Add(InitFromObject(SerializationHelper.Instance.SearchElementTypeForList(t), re));

            return list;
        }

        if (e is RubyHash rh)
        {
            if (!typeof(IDictionary).IsAssignableFrom(t))
                throw new Exception("Type does not implement IList");

            var dict = Activator.CreateInstance(t) as IDictionary;

            foreach (var re in rh.Pairs)
            {
                var types = SerializationHelper.Instance.SearchElementTypesForDictionary(t);

                dict.Add(InitFromObject(types.Item1, re.Key), InitFromObject(types.Item2, re.Value));
            }

            return dict;
        }

        if (e is RubyUserDefined ru)
        {
            if (t == typeof(object))
                return ru;

            var objectName = ru.GetRealClassName();
            if (_classMap.ContainsKey(objectName))
                return DeserializeUserDefinedObject(_classMap[objectName], ru);

            throw new Exception($"Unsupported user-defined object [{objectName}]");
        }

        if (e is RubyObject ro)
        {
            if (t == typeof(object))
                return ro;

            var objectName = ro.GetRealClassName();
            if (_classMap.ContainsKey(objectName))
            {
                var type = _classMap[objectName];

                return DeserializeObject(type, ro);
            }

            throw new Exception($"Unsupported object [{objectName}]");
        }

        throw new Exception($"Unsupported [{e.GetType()}]");
    }

    private static object DeserializeUserDefinedObject(Type type, RubyUserDefined data)
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
