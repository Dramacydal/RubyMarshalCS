namespace RubyMarshalCS.Settings;

public class SerializationSettings
{
    public string ContextTag { get; set; } = "";
    public bool ResolveLinks { get; set; } = false;
    public bool EnsureObjects { get; set; } = true;
    public bool EnsureExtensionDataPresent { get; set; } = true;
    public bool WriteTrashProperties { get; set; } = true;
    public bool AllowGenericUserObjects { get; set; } = false;
}
