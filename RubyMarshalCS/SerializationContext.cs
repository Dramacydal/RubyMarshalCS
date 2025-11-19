using RubyMarshalCS.Enums;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Helper;

namespace RubyMarshalCS;

public class SerializationContext
{
    public RubyNil Nil { get; private set; }
    public RubyTrue True { get; private set; }
    public RubyFalse False { get; private set; }

    public SerializationContext()
    {
        Nil = Create<RubyNil>();
        True = Create<RubyTrue>();
        False = Create<RubyFalse>();
    }

    private int objectIdCounter = 0;
    private int symbolIdCounter = 0;
    
    private readonly TwoWayDictionary<int, AbstractEntity> _objectInstances = new();
    private readonly TwoWayDictionary<int, RubySymbol> _symbolInstances = new();

    public void RememberObject(AbstractEntity entity)
    {
        _objectInstances.Add(objectIdCounter, entity);
        ++objectIdCounter;
    }

    public void RememberSymbol(RubySymbol entity)
    {
        _symbolInstances.Add(symbolIdCounter, entity);
        ++symbolIdCounter;
    }

    public RubySymbol LookupRememberedSymbol(AbstractEntity symbolOrLink)
    {
        if (symbolOrLink.Code == RubyCodes.Symbol)
            return (RubySymbol)symbolOrLink;
        if (symbolOrLink.Code == RubyCodes.SymbolLink)
        {
            var sl = (RubySymbolLink)symbolOrLink;

            if (_symbolInstances.TryGetValue(sl.ReferenceId, out var symbol))
                return (RubySymbol)symbol!;

            throw new Exception("Symbol link is out of bounds");
        }

        throw new Exception("Entity is not symbol or link");
    }

    public AbstractEntity LookupRememberedObject(AbstractEntity objectOrLink)
    {
        if (objectOrLink.Code == RubyCodes.ObjectLink)
        {
            var ol = (RubyObjectLink)objectOrLink;
            if (_objectInstances.TryGetValue(ol.ReferenceId, out var obj))
                return obj;

            throw new Exception("Object link is out of bounds");
        }

        return objectOrLink;
    }
    
    public bool LookupStoredObjectIndex(AbstractEntity entity, out int id) => _objectInstances.TryGetValue(entity, out id);
    
    public bool LookupStoredSymbolIndex(RubySymbol entity, out int id) => _symbolInstances.TryGetValue(entity, out id);

    public T Create<T>() where T: AbstractEntity, new()
    {
        var e = new T
        {
            Context = this
        };

        return e;
    }

    public void Reset()
    {
        objectIdCounter = symbolIdCounter = 0;
        _objectInstances.Clear();
        _symbolInstances.Clear();
    }
}
