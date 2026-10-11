namespace RubyMarshalCS.Serialization;

internal static class ValueWrapper
{
    public static void SetValue(Candidate cand, object obj, object? value, bool allowDynamic = false)
    {
        if (cand.Converter != null)
        {
            cand.SetValue(obj, cand.Converter.FromRuby(value, obj));
            return;
        }

        cand.SetValue(obj,
            SerializationHelper.AssignmentConversion(cand.ValueType, value, allowDynamic));
    }
}
