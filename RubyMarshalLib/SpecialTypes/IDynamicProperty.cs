namespace RubyMarshalCS.SpecialTypes;

public interface IDynamicProperty
{
    public void Set(object? val);
    public object? Get();
}