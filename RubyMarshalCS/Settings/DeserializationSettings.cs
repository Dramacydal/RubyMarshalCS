using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Settings;

public class DeserializationSettings
{
    // Serialization context tag 
    public string ContextTag { get; set; } = "";

    // Object and symbol links will be resolved immediately
    public bool ResolveLinks { get; set; }

    // Check object properties are In/Out only
    public bool ConsiderInOutFields { get; set; } = true;

    public MissingMemberHandling MissingMemberHandling { get; set; } = MissingMemberHandling.Error;

    public EventHandler<MissingMemberArgs>? MissingMember;

    // Undefined ruby objects handling - store as generic object or raise an error
    public UndefinedObjectHandling UndefinedObjectHandling { get; set; } = UndefinedObjectHandling.Error;

    public EventHandler<UndefinedObjectArgs>? UndefinedObject;

    // User objects handling - store as generic object or raise an error if not serializer defined
    public UndefinedUserObjectHandling UndefinedUserObjectHandling { get; set; } = UndefinedUserObjectHandling.Error;

    public EventHandler<UndefinedUserObjectArgs>? UndefinedUserObject;

    // Ensure all data is read from stream after deserialization
    public bool EnsureReadToEnd { get; set; }
}
