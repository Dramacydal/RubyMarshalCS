using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public abstract class AbstractEntity
{
    public SerializationContext Context { get; set; }

    public abstract RubyCodes Code { get; }
    
    private List<AbstractEntity>? _modules;
    public List<AbstractEntity> Modules => _modules ??= new();
    internal IReadOnlyList<AbstractEntity> ModulesOrEmpty => (IReadOnlyList<AbstractEntity>?)_modules ?? Array.Empty<AbstractEntity>();

    public AbstractEntity? UserClass;

    private List<KeyValuePair<AbstractEntity, AbstractEntity>>? _ivars;
    public List<KeyValuePair<AbstractEntity, AbstractEntity>> InstanceVariables => _ivars ??= new();
    internal IReadOnlyList<KeyValuePair<AbstractEntity, AbstractEntity>> InstanceVariablesOrEmpty => (IReadOnlyList<KeyValuePair<AbstractEntity, AbstractEntity>>?)_ivars ?? Array.Empty<KeyValuePair<AbstractEntity, AbstractEntity>>();
    
    public AbstractEntity ResolveIfLink()
    {
        return Code switch
        {
            RubyCodes.SymbolLink => Context.LookupRememberedSymbol(this),
            RubyCodes.ObjectLink => Context.LookupRememberedObject(this),
            _ => this
        };
    }
}
