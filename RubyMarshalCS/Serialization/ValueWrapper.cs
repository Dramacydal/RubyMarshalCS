namespace RubyMarshalCS.Serialization;

internal static class ValueWrapper
{
    public static void SetValue(Candidate cand, object obj, object? value, bool allowDynamic = false)
    {
        cand.SetValue(obj,
            SerializationHelper.AssignmentConversion(cand.ValueType, value, allowDynamic));
    }
}
