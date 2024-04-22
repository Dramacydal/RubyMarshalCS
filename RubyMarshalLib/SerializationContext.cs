using RubyMarshal.Entities;
using RubyMarshal.Enums;
using RubyMarshal.Settings;

namespace RubyMarshal;

public class SerializationContext
{
    private readonly List<RubySymbol> _symbolInstances = new();
    private readonly List<AbstractEntity> _objectInstances = new();

    private readonly ReaderSettings _settings = new();

    public SerializationContext(ReaderSettings settings = null)
    {
        _settings = settings ?? _settings;
    }

    public RubySymbol LookupSymbol(AbstractEntity symbolOrLink)
    {
        if (symbolOrLink is RubySymbol s)
            return s;
        if (symbolOrLink is RubySymbolLink sl)
            return sl.ReferenceId < _symbolInstances.Count ? _symbolInstances[sl.ReferenceId] : null;

        throw new Exception("Entity is not symbol or link");
    }

    public AbstractEntity LookupObject(AbstractEntity objectOrLink)
    {
        if (objectOrLink is RubyObjectLink ol)
            return ol.ReferenceId < _objectInstances.Count ? _objectInstances[ol.ReferenceId] : null;

        return objectOrLink;
    }

    private static readonly List<RubyCodes> LinkableObjectTypes = new()
    {
        RubyCodes.Array,
        RubyCodes.Hash,
        RubyCodes.InstanceVar,
        RubyCodes.Object,
        RubyCodes.Float,
        RubyCodes.String,
        RubyCodes.UserDefined,
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
        [RubyCodes.Object] = typeof(RubyObject),
        [RubyCodes.UserDefined] = typeof(RubyUserDefined),
        [RubyCodes.Hash] = typeof(RubyHash),
    };

    public AbstractEntity Create(RubyCodes code)
    {
        if (!CodeToObjectTypeMap.TryGetValue(code, out var type))
            throw new Exception($"Unsupported code {code}");

        var e = Activator.CreateInstance(type) as AbstractEntity;
        e!.Context = this;

        if (code == RubyCodes.Symbol)
            _symbolInstances.Add(e as RubySymbol);
        else if (LinkableObjectTypes.Contains(code))
            _objectInstances.Add(e);

        return e;
    }

    public AbstractEntity Read(BinaryReader reader)
    {
        var code = reader.ReadByte();
        var e = Create((RubyCodes)code);
        e.ReadData(reader);

        if (_settings.ResolveLinks)
        {
            if (e is RubyObjectLink ol)
                e = LookupObject(ol);
            if (e is RubySymbolLink sl)
                e = LookupSymbol(e);
        }

        return e;
    }

    public void Write(BinaryWriter writer, AbstractEntity entity)
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

        if (entity is RubySymbol rs)
        {
            var index = _symbolInstances.IndexOf(rs);
            if (index != -1)
            {
                Write(writer, new RubySymbolLink() { ReferenceId = index });
                return;
            }

            _symbolInstances.Add(rs);
        }

        writer.Write((byte)entity.Code);
        entity.WriteData(writer);
    }
}
