namespace RubyMarshalCS.SpecialTypes;

public class DynamicProperty<T1, T2> : AbstractDynamicProperty
{
    public DynamicProperty()
    {
        AddVariant<T1>();
        AddVariant<T2>();
    }
}

public class DynamicProperty<T1, T2, T3> : AbstractDynamicProperty
{
    public DynamicProperty()
    {
        AddVariant<T1>();
        AddVariant<T2>();
        AddVariant<T3>();
    }
}

public class DynamicProperty<T1, T2, T3, T4> : AbstractDynamicProperty
{
    public DynamicProperty()
    {
        AddVariant<T1>();
        AddVariant<T2>();
        AddVariant<T3>();
        AddVariant<T4>();
    }
}
