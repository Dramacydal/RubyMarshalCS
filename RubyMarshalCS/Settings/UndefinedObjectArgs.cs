using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Settings;

public class UndefinedObjectArgs
{
    public required RubyObject RubyObject { get; init; }

    public UndefinedObjectHandling Action { get; private set; } = UndefinedObjectHandling.Error;

    public void Ignore() => Action = UndefinedObjectHandling.Ignore;
}
