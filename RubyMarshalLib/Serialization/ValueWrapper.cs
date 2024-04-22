using System.Reflection;

namespace RubyMarshal.Serialization;

class ValueWrapper
{
    public object Object { get; set; }
    public PropertyInfo? Property { get; set; }
    public FieldInfo? Field { get; set; }

    public Type Type => Property?.PropertyType ?? Field?.FieldType;

    public void SetValue(object Value, bool allowDynamic = false)
    {
        if (Property != null)
            Property?.SetValue(Object, SerializationHelper.AssignmentConversion(Property.PropertyType, Value, allowDynamic));

        if (Field != null)
            Field?.SetValue(Object, SerializationHelper.AssignmentConversion(Field.FieldType, Value, allowDynamic));
    }
}
