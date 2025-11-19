using RubyMarshalCS.Enums;
using RubyMarshalCS.Entities;

namespace RubyMarshalCS;

public class SerializationContext
{
    private static readonly Dictionary<RubyCodes, Type> CodeToObjectTypeMap = new()
    {
        [RubyCodes.String] = typeof(RubyString),
        [RubyCodes.RegExp] = typeof(RubyRegExp),
        [RubyCodes.Nil] = typeof(RubyNil),
        [RubyCodes.Symbol] = typeof(RubySymbol),
        [RubyCodes.SymbolLink] = typeof(RubySymbolLink),
        [RubyCodes.ObjectLink] = typeof(RubyObjectLink),
        [RubyCodes.False] = typeof(RubyFalse),
        [RubyCodes.True] = typeof(RubyTrue),
        [RubyCodes.Array] = typeof(RubyArray),
        [RubyCodes.Float] = typeof(RubyFloat),
        [RubyCodes.FixNum] = typeof(RubyFixNum),
        [RubyCodes.BigNum] = typeof(RubyBigNum),
        [RubyCodes.Object] = typeof(RubyObject),
        [RubyCodes.Struct] = typeof(RubyStruct),
        [RubyCodes.Class] = typeof(RubyClass),
        [RubyCodes.Module] = typeof(RubyModule),
        [RubyCodes.ModuleOld] = typeof(RubyModuleOld),
        [RubyCodes.UserDefined] = typeof(RubyUserDefined),
        [RubyCodes.UserMarshal] = typeof(RubyUserMarshal),
        [RubyCodes.Hash] = typeof(RubyHash),
        [RubyCodes.Data] = typeof(RubyData),
    };

    private readonly List<AbstractEntity> _objectInstances = new();
    private readonly List<AbstractEntity> _symbolInstances = new();
    public readonly List<AbstractEntity> _allObjects = new();

    public void RememberObject(AbstractEntity entity)
    {
        _objectInstances.Add(entity);
    }
    
    public void RememberSymbol(AbstractEntity entity)
    {
        _symbolInstances.Add(entity);
    }

    public RubySymbol LookupRememberedSymbol(AbstractEntity symbolOrLink)
    {
        if (symbolOrLink.Code == RubyCodes.Symbol)
            return (RubySymbol)symbolOrLink;
        if (symbolOrLink.Code == RubyCodes.SymbolLink)
        {
            var sl = (RubySymbolLink)symbolOrLink;

            if (sl.ReferenceId < _symbolInstances.Count)
                return (RubySymbol)_symbolInstances[sl.ReferenceId];
            throw new Exception("Symbol link is out of bounds");
        }

        throw new Exception("Entity is not symbol or link");
    }

    public AbstractEntity LookupRememberedObject(AbstractEntity objectOrLink)
    {
        if (objectOrLink.Code == RubyCodes.ObjectLink)
        {
            var ol = (RubyObjectLink)objectOrLink;
            if (ol.ReferenceId < _objectInstances.Count)
                return _objectInstances[ol.ReferenceId];

            throw new Exception("Object link is out of bounds");
        }

        return objectOrLink;
    }
    
    public int LookupStoredObjectIndex(AbstractEntity entity) => _objectInstances.IndexOf(entity);
    
    public int LookupStoredSymbolIndex(AbstractEntity entity) => _symbolInstances.IndexOf(entity);

    public T Create<T>() where T: AbstractEntity, new()
    {
        var e = new T();

        e.Context = this;

        _allObjects.Add(e);

        return e;
    }

    public void Reset()
    {
        _symbolInstances.Clear();
        _objectInstances.Clear();
        _allObjects.Clear();
    }
}
