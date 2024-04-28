namespace RubyMarshalCS.Conversion.Interfaces;

public interface ICustomConverter
{
    public bool CanConvert(object o, Type type);

    public object Convert(object value, Type type);
}
