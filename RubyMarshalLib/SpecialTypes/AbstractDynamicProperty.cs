namespace RubyMarshal.SpecialTypes;

public abstract class AbstractDynamicProperty : IDynamicProperty
{
    protected readonly Dictionary<Type, object?> _valueHolder = new();

    private Type? _type = null;

    private bool _isNull = true;

    public void Set(object? val)
    {
        if (val == null)
        {
            _isNull = true;
            _type = null;
            return;
        }

        _isNull = false;

        var valueType = val.GetType();
        if (!_valueHolder.ContainsKey(valueType))
            throw new Exception($"Type {valueType} is not supported by {GetType()}");

        foreach (var key in _valueHolder.Keys)
        {
            if (key == valueType)
            {
                _valueHolder[key] = val;
                _type = valueType;
            }
            else
                _valueHolder[key] = key.IsGenericType ? Activator.CreateInstance(key) : null;
        }
    }

    public object? Get()
    {
        if (_isNull)
            return null;

        return _valueHolder[_type];
    }
}
