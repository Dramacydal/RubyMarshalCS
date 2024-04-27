namespace RubyMarshalCS.SpecialTypes;

public abstract class AbstractCustomConverter<T1, T2> : ICustomConverter
{
    public bool CanConvert(object o, Type type)
    {
        return o.GetType() == typeof(T1) && type == typeof(T2) || o.GetType() == typeof(T2) && type == typeof(T1);
    }

    protected abstract void Convert(T1 from, out T2 to);
    protected abstract void Convert(T2 from, out T1 to);

    public object Convert(object value, Type type)
    {
        if (type == typeof(T2))
        {
            Convert((T1)value, out var val);

            return val!;
        }

        if (type == typeof(T1))
        {
            Convert((T2)value, out var val);

            return val!;
        }

        throw new Exception($"Object value must be of type {typeof(T1)} or {typeof(T2)}");
    }
}
