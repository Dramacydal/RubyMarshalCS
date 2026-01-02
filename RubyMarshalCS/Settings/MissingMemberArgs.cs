using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Settings;

public class MissingMemberArgs
{
    public class MissingMember
    {
        public required string Key { get; init; }

        public required object? Value { get; init; }
    }

    public required RubyObject RubyObject { get; init; }
    
    public required object Object { get; init; }

    public required MissingMember Member { get; init; }

    public MissingMemberHandling Action { get; private set; } = MissingMemberHandling.Error;

    public void Store() => Action = MissingMemberHandling.Store;

    public void Ignore() => Action = MissingMemberHandling.Ignore;
}
