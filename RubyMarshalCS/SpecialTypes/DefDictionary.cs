using RubyMarshalCS.SpecialTypes.Interfaces;

namespace RubyMarshalCS.SpecialTypes;

public class DefDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDefDictionary where TKey : notnull
{
    public object? DefaultValue { get; set; } = default(TValue);
}

public class DefDictionary<TKey, TValue, TDefaultInitializer> : Dictionary<TKey, TValue>, IDefDictionary
    where TDefaultInitializer : IDefaultValueInitializer where TKey : notnull
{
    public object? DefaultValue { get; set; } = InitDefaultValue();

    private static object? InitDefaultValue()
    {
        var initializer = (TDefaultInitializer)Activator.CreateInstance(typeof(TDefaultInitializer))!;

        return initializer.DefaultValue;
    }
}
