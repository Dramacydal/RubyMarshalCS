namespace RubyMarshal.SpecialTypes;

public interface ICustomConverter
{
    public bool CanConvert(object o, Type type);

    public object Convert(object value, Type type);
}
