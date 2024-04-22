namespace RubyMarshal.SpecialTypes;

public class DynamicProperty<T1, T2> : AbstractDynamicProperty
{
    public DynamicProperty()
    {
        _valueHolder.Add(typeof(T1), default(T1));
        _valueHolder.Add(typeof(T2), default(T2));
    }
}

public class DynamicProperty<T1, T2, T3> : AbstractDynamicProperty
{
    public DynamicProperty()
    {
        _valueHolder.Add(typeof(T1), default(T1));
        _valueHolder.Add(typeof(T2), default(T2));
        _valueHolder.Add(typeof(T3), default(T3));
    }
}

public class DynamicProperty<T1, T2, T3, T4> : AbstractDynamicProperty
{
    public DynamicProperty()
    {
        _valueHolder.Add(typeof(T1), default(T1));
        _valueHolder.Add(typeof(T2), default(T2));
        _valueHolder.Add(typeof(T3), default(T3));
        _valueHolder.Add(typeof(T4), default(T4));
    }
}
