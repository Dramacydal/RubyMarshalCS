namespace RubyMarshal.SpecialTypes;

public abstract class AbstractCustomConverter<T, V> : ICustomConverter
{
    public bool CanConvert(object o, Type type)
    {
        return o.GetType() == typeof(T) && type == typeof(V) || o.GetType() == typeof(V) && type == typeof(T);
    }

    protected abstract void Convert(T from, out V to);
    protected abstract void Convert(V from, out T to);

    public object Convert(object value, Type type)
    {
        if (type == typeof(V))
        {
            Convert((T)value, out var val);

            return val;
        }

        if (type == typeof(T))
        {
            Convert((V)value, out var val);

            return val;
        }

        throw new Exception("");
    }
}
