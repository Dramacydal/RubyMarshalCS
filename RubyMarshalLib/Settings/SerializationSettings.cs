namespace RubyMarshalCS.Settings;

public class SerializationSettings
{
    public bool ResolveLinks { get; set; } = false;
    public bool EnsureObjects { get; set; } = true;
    public bool EnsureExtensionDataPresent { get; set; } = true;
    public bool WriteTrashProperties { get; set; } = true;
}
