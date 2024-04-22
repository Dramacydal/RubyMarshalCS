using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using RubyMarshal.SpecialTypes;

namespace RubyMarshal.Serialization;

class ValueWrapper
{
    public object Object { get; set; }
    public PropertyInfo? Property { get; set; }
    public FieldInfo? Field { get; set; }

    public Type Type => Property?.PropertyType ?? Field?.FieldType;

    public void SetValue(object Value, bool allowDynamic = false)
    {
        if (Value is IList list)
        {
            AddListElements(list, allowDynamic);
            return;
        }

        if (Value is IDictionary dictionary)
        {
            AddDictionaryElements(dictionary, allowDynamic);
            return;
        }


        if (Property != null)
        {
            if (Property.PropertyType != typeof(object) && Property.PropertyType != Value.GetType())
                Property?.SetValue(Object, ManualCast(Property.PropertyType,Value));
            else
                Property?.SetValue(Object, Value);
        }

        if (Field != null)
        {
            if (Field.FieldType != typeof(object) && Field.FieldType != Value.GetType())
                Field?.SetValue(Object, ManualCast(Field.FieldType,Value));
            else
                Field?.SetValue(Object, Value);
        }
    }

    private static object ManualCast(Type type, object o)
    {
        if (o.GetType() != typeof(SpecialString))
        {
            Debug.WriteLine("");
        }
        
        var dataParam = Expression.Parameter(typeof(object), "data");
        var body = Expression.Block(Expression.Convert(Expression.Convert(dataParam, o.GetType()), type));

        var run = Expression.Lambda(body, dataParam).Compile();

        return run.DynamicInvoke(o);
    }

    private void AddListElements(IList values, bool allowDynamic)
    {
        IList? list = null;

        if (Property != null)
        {
            if (typeof(IList).IsAssignableFrom(Property.PropertyType))
            {
                list = Property.GetValue(Object) as IList;
                if (list == null)
                {
                    list = Activator.CreateInstance(Property.PropertyType) as IList;
                    Property.SetValue(Object, list);
                }
            }
            else if (Property.PropertyType == typeof(object) && allowDynamic)
            {
                list = new List<object>();
                Property.SetValue(Object, list);
            }
            else
                throw new Exception($"Property [{Property.Name}] does not implement IList interface");
        }

        if (Field != null)
        {
            if (typeof(IList).IsAssignableFrom(Field.FieldType))
            {
                list = Field.GetValue(Object) as IList;
                if (list == null)
                {
                    list = Activator.CreateInstance(Field.FieldType) as IList;
                    Field.SetValue(Object, list);
                }
            }
            else if (Field.FieldType == typeof(object) && allowDynamic)
            {
                list = new List<object>();
                Field.SetValue(Object, list);
            }
            else
                throw new Exception($"Field [{Field.Name}] does not implement IList interface");
        }

        foreach (var e in values)
            list?.Add(e);
    }

    private void AddDictionaryElements(IDictionary values, bool allowDynamic)
    {
        IDictionary? dictionary = null;
        if (Property != null)
        {
            if (typeof(IDictionary).IsAssignableFrom(Property.PropertyType))
            {
                dictionary = Property.GetValue(Object) as IDictionary;
                if (dictionary == null)
                {
                    dictionary = Activator.CreateInstance(Property.PropertyType) as IDictionary;
                    Property.SetValue(Object, dictionary);
                }
            }
            else if (Property.PropertyType == typeof(object) && allowDynamic)
            {
                dictionary = new Dictionary<object, object>();
                Property.SetValue(Object, dictionary);
            }
            else
                throw new Exception($"Property [{Property.Name}] does not implement IDictionary interface");
        }

        if (Field != null)
        {
            if (typeof(IDictionary).IsAssignableFrom(Field.FieldType))
            {
                dictionary = Field.GetValue(Object) as IDictionary;
                if (dictionary == null)
                {
                    dictionary = Activator.CreateInstance(Field.FieldType) as IDictionary;
                    Field.SetValue(Object, dictionary);
                }
            }
            else if (Field.FieldType == typeof(object) && allowDynamic)
            {
                dictionary = new Dictionary<object, object>();
                Field.SetValue(Object, dictionary);
            }
            else
                throw new Exception($"Field [{Field.Name}] does not implement IDictionary interface");
        }
        
        foreach (DictionaryEntry e in values)
            dictionary?.Add(e.Key, e.Value);
    }
}
