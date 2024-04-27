namespace RubyMarshalCS.SpecialTypes;

public abstract class AbstractDynamicProperty : IDynamicProperty
{
    private readonly Dictionary<Type, object?> _valueHolder = new();

    private Type? _type;

    private bool _isNull = true;

    protected void AddVariant<T>()
    {
        _valueHolder.Add(typeof(T), default(T));
    }

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
        return _isNull ? null : _valueHolder[_type!];
    }
}
