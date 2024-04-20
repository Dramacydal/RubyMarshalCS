using System.Collections;
using System.Reflection;
using System.Text;
using RubyMarshal.Entities;
using RubyMarshal.Serialization;

namespace RubyMarshal;

public static class RubyConverter
{
    private static Dictionary<string, Type> _classMap = new();

    static RubyConverter()
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        foreach (var t in a.GetTypes())
        foreach (var attr in t.GetCustomAttributes(typeof(RubyObjectAttribute)))
            RegisterClass((attr as RubyObjectAttribute).Name, t);
    }

    public static void RegisterClass(string name, Type type)
    {
        if (_classMap.ContainsKey(name))
            throw new Exception($"Class [{name}] already registered");

        _classMap[name] = type;
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

    private static T DeserializeObject<T>(RubyObject data)
    {
        var type = typeof(T);

        var obj = Activator.CreateInstance<T>();

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

            continue;
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
            return ru;
        
        if (e is RubyObject ro)
        {
            var objectName = ro.GetRealClassName().ToString();
            if (_classMap.ContainsKey(objectName))
            {
                var type = _classMap[objectName];

                // foreach (MethodInfo mi in typeof(RubyConverter).GetMethods())
                // {
                //     if (mi.Name == nameof(Deserialize))
                //     {
                //         var args = mi.GetGenericArguments();
                //     }
                //     
                // }

                var m = typeof(RubyConverter).GetMethod(nameof(DeserializeObject), BindingFlags.Static | BindingFlags.NonPublic, new[] { typeof(RubyObject) });


                return m.MakeGenericMethod(type).Invoke(null, new[] { ro });
            }
            
            throw new Exception($"Unsupported object [{objectName}]");
        }

        throw new Exception($"Unsupported [{e.GetType()}]");
    }
}
