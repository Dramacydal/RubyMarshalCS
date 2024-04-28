using System.Reflection;

namespace RubyMarshalCS.Serialization;

class ValueWrapper
{
    private readonly object _object;
    private readonly PropertyInfo? _property;
    private readonly FieldInfo? _field;

    public ValueWrapper(object obj, PropertyInfo property)
    {
        _object = obj;
        _property = property;
    }

    public ValueWrapper(object obj, FieldInfo field)
    {
        _object = obj;
        _field = field;
    }

    public void SetValue(object? value, bool allowDynamic = false)
    {
        if (_property != null)
            _property?.SetValue(_object, SerializationHelper.AssignmentConversion(_property.PropertyType, value, allowDynamic));

        if (_field != null)
            _field?.SetValue(_object, SerializationHelper.AssignmentConversion(_field.FieldType, value, allowDynamic));
    }
}
