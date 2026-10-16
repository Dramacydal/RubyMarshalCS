using RubyMarshalCS.Entities;

namespace RubyMarshalCS;

public class ReaderContext
{
    public bool HasIVars { get; set; }

    /// <summary>The instance variables that follow the value are read already (by the value itself); they are read once.</summary>
    public bool IVarsRead { get; set; }

    public Func<ReaderContext, AbstractEntity> Action { get; set; }

    private ReaderContext MakeSubContext(bool hasIVars)
    {
        return new()
        {
            Action = Action,
            HasIVars = hasIVars,
        };
    }

    internal ReaderContext WithoutIVars() => MakeSubContext(false);

    public AbstractEntity WithSubContext(bool hasIVars, Func<ReaderContext, AbstractEntity> action)
    {
        return action(MakeSubContext(hasIVars));
    }

    public void WithSubContext(bool hasIVars, Action<ReaderContext> action)
    {
        action(MakeSubContext(hasIVars));
    }
}
