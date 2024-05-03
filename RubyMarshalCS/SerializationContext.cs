using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;
using RubyMarshalCS.Settings;

namespace RubyMarshalCS;

public class SerializationContext
{
    private static readonly List<RubyCodes> LinkableObjectTypes = new()
    {
        RubyCodes.Array,
        RubyCodes.Hash,
        RubyCodes.HashDef,
        RubyCodes.InstanceVar,
        RubyCodes.Object,
        RubyCodes.Class,
        RubyCodes.Extended,
        RubyCodes.Float,
        RubyCodes.BigNum,
        RubyCodes.String,
        RubyCodes.UserDefined,
        RubyCodes.UserMarshal,
    };

    private static readonly Dictionary<RubyCodes, Type> CodeToObjectTypeMap = new()
    {
        [RubyCodes.String] = typeof(RubyString),
        [RubyCodes.Nil] = typeof(RubyNil),
        [RubyCodes.Symbol] = typeof(RubySymbol),
        [RubyCodes.SymbolLink] = typeof(RubySymbolLink),
        [RubyCodes.ObjectLink] = typeof(RubyObjectLink),
        [RubyCodes.False] = typeof(RubyFalse),
        [RubyCodes.InstanceVar] = typeof(RubyInstanceVariable),
        [RubyCodes.True] = typeof(RubyTrue),
        [RubyCodes.Array] = typeof(RubyArray),
        [RubyCodes.Float] = typeof(RubyFloat),
        [RubyCodes.FixNum] = typeof(RubyFixNum),
        [RubyCodes.BigNum] = typeof(RubyBigNum),
        [RubyCodes.Object] = typeof(RubyObject),
        [RubyCodes.Class] = typeof(RubyClass),
        [RubyCodes.Extended] = typeof(RubyExtended),
        [RubyCodes.UserDefined] = typeof(RubyUserDefined),
        [RubyCodes.UserMarshal] = typeof(RubyUserMarshal),
        [RubyCodes.Hash] = typeof(RubyHash),
        [RubyCodes.HashDef] = typeof(RubyHashDef),
    };

    private readonly List<AbstractEntity> _objectInstances = new();
    private readonly List<AbstractEntity> _objectLinks = new();

    private readonly SerializationSettings _settings = new();
    private readonly List<AbstractEntity> _symbolInstances = new();
    private readonly List<AbstractEntity> _symbolLinks = new();

    public SerializationContext(SerializationSettings? settings = null)
    {
        _settings = settings ?? _settings;
    }

    public RubySymbol LookupSymbol(AbstractEntity symbolOrLink)
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

    public AbstractEntity LookupObject(AbstractEntity objectOrLink)
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

    public AbstractEntity Create(RubyCodes code, bool skipObjectStore = false)
    {
        if (!CodeToObjectTypeMap.TryGetValue(code, out var type))
            throw new Exception($"Unsupported code {code}");

        var e = Activator.CreateInstance(type) as AbstractEntity;
        e!.Context = this;

        if (code == RubyCodes.Symbol)
            _symbolInstances.Add(e);
        else if (!skipObjectStore && LinkableObjectTypes.Contains(code))
            _objectInstances.Add(e);
        
        if (e.Code == RubyCodes.ObjectLink)
            _objectLinks.Add(e);
        else if (e.Code == RubyCodes.SymbolLink)
            _symbolLinks.Add(e);

        return e;
    }

    public AbstractEntity Read(BinaryReader reader, bool skipObjectStore = false)
    {
        var code = reader.ReadByte();
        var e = Create((RubyCodes)code, skipObjectStore);
        e.ReadData(reader);

        if (_settings.ResolveLinks)
        {
            if (e.Code == RubyCodes.ObjectLink)
                e = LookupObject(e);
            else if (e.Code == RubyCodes.SymbolLink)
                e = LookupSymbol(e);
        }

        return e;
    }

    public void Write(BinaryWriter writer, AbstractEntity entity, bool skipObjectStore = false)
    {
        if (!skipObjectStore)
        {
            if (LinkableObjectTypes.Contains(entity.Code))
            {
                var index = _objectInstances.IndexOf(entity);
                if (index != -1)
                {
                    Write(writer, new RubyObjectLink() { ReferenceId = index });
                    return;
                }

                _objectInstances.Add(entity);
            }
            else if (entity.Code == RubyCodes.Symbol)
            {
                var index = _symbolInstances.IndexOf((RubySymbol)entity);
                if (index != -1)
                {
                    Write(writer, new RubySymbolLink() { ReferenceId = index });
                    return;
                }

                _symbolInstances.Add(entity);
            }
        }

        writer.Write((byte)entity.Code);
        entity.WriteData(writer);
    }

    public void Reset()
    {
        _symbolInstances.Clear();
        _symbolLinks.Clear();
        
        _objectInstances.Clear();
        _objectLinks.Clear();
    }
}
