using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Settings;

public class UndefinedUserObjectArgs
{
    public required RubyUserDefined RubyUserObject { get; init; }
    
    public UndefinedUserObjectHandling Action { get; private set; } = UndefinedUserObjectHandling.Error;

    public void Store() => Action = UndefinedUserObjectHandling.Store;

    public void Ignore() => Action = UndefinedUserObjectHandling.Ignore;
}
