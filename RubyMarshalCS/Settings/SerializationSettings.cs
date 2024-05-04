namespace RubyMarshalCS.Settings;

public class SerializationSettings
{
    // Serialization context tag
    public string ContextTag { get; set; } = "";
    
    // Check object properties are In/Out only
    public bool ConsiderInOutFields { get; set; } = true;
}
