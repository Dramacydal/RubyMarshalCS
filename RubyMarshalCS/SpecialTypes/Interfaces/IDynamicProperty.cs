namespace RubyMarshalCS.SpecialTypes.Interfaces;

public interface IDynamicProperty
{
    public void Set(object? val);
    public object? Get();
}