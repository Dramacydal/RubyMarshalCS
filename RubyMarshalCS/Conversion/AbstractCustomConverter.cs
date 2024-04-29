using RubyMarshalCS.Conversion.Interfaces;

namespace RubyMarshalCS.Conversion;

public abstract class AbstractCustomConverter<TSource, TTarget> : ICustomConverter
{
    public bool CanConvert(object o, Type type)
    {
        return o.GetType() == typeof(TSource) && type == typeof(TTarget) ||
               o.GetType() == typeof(TTarget) && type == typeof(TSource);
    }

    public bool CanConvertBack(object o)
    {
        return o.GetType() == typeof(TTarget);
    }

    protected abstract void Convert(TSource from, out TTarget to);
    protected abstract void Convert(TTarget from, out TSource to);

    public object Convert(object value, Type type)
    {
        if (type == typeof(TTarget))
        {
            Convert((TSource)value, out var val);

            return val!;
        }

        if (type == typeof(TSource))
        {
            Convert((TTarget)value, out var val);

            return val!;
        }

        throw new Exception($"Object value must be of type {typeof(TSource)} or {typeof(TTarget)}");
    }

    public object ConvertBack(object value)
    {
        return Convert(value, typeof(TSource));
    }
}
