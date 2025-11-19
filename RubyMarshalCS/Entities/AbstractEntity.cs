using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Entities;

public abstract class AbstractEntity
{
    public SerializationContext Context { get; set; }

    public abstract RubyCodes Code { get; protected set; }
    
    public readonly List<AbstractEntity> Modules = new();

    public AbstractEntity? UserClass;

    public readonly List<KeyValuePair<AbstractEntity, AbstractEntity>> InstanceVariables = new();
    
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
