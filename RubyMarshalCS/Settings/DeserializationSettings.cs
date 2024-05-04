namespace RubyMarshalCS.Settings;

public class DeserializationSettings
{
    // Serialization context tag 
    public string ContextTag { get; set; } = "";
    
    // Object and symbol links will be resolved immediately
    public bool ResolveLinks { get; set; }

    // Check object properties are In/Out only
    public bool ConsiderInOutFields { get; set; } = true;
    
    // Check all objects have extension data field
    public bool EnsureExtensionFieldPresent { get; set; } = true;

    // Allow fields not mapped into objects (they go to extension field)
    public bool AllowUnmappedFields { get; set; } = true;
    
    // Ruby objects with user defined serializer with be read as generic if no serializer defined
    public bool AllowGenericUserObjects { get; set; } = false;

    // Ensure all data is read from stream after deserialization
    public bool EnsureReadToEnd { get; set; }
}
