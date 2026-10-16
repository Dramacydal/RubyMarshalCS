using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;

namespace RubyMarshalCS.Tests;

/// <summary>
/// A description of the value that the entities of the reader hold, written as <c>Data/generate/canon.rb</c> writes it
/// from the objects of Ruby itself: the kinds, the bytes, the numbers, the order, the instance variables, and which objects are the same
/// object (an object that is met again is written as @number of its first visit). Equal digests mean that the reader holds
/// exactly what Ruby has. The two descriptions have to be changed together.
/// </summary>
internal sealed class RubyMarshalDescription
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private readonly Dictionary<AbstractEntity, int> _ids = new(ReferenceEqualityComparer.Instance);

    /// <summary>The number of lines of the description.</summary>
    public int Nodes { get; private set; }

    /// <summary>How many objects were met more than once, and how many of them at different depths of nesting.</summary>
    public (int Shared, int AtDifferentDepths) Sharing => (_visits.Count(p => p.Value.Visits > 1), _visits.Count(p => p.Value.Visits > 1 && p.Value.Min != p.Value.Max));

    public int MaxDepth { get; private set; }

    private readonly Dictionary<AbstractEntity, (int Visits, int Min, int Max)> _visits = new(ReferenceEqualityComparer.Instance);

    public static RubyMarshalDescription Of(AbstractEntity entity)
    {
        var description = new RubyMarshalDescription();
        description.Walk(entity, 0);

        return description;
    }

    /// <summary>The first 12 characters of the hash, as <c>canon.rb</c> writes them.</summary>
    public string Digest => Convert.ToHexString(_hash.GetHashAndReset()).ToLowerInvariant()[..12];

    private void Line(string text)
    {
        _hash.AppendData(Encoding.UTF8.GetBytes(text + "\n"));
        Nodes++;
    }

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static string Name(AbstractEntity? entity) => entity == null ? "" : entity.ResolveIfLink().ToString()!;

    private static bool IsEncoding(AbstractEntity key) => Name(key) is "E" or "encoding";

    private static string UserClass(AbstractEntity entity, string baseName)
    {
        var name = entity.UserClass == null ? "" : Name(entity.UserClass);

        return name == baseName ? "" : name;
    }

    private void Modules(AbstractEntity entity)
    {
        if (entity.Modules.Count > 0)
            Line("e:" + string.Join(",", entity.Modules.Select(Name).Order(StringComparer.Ordinal)));
    }

    private void InstanceVariables(AbstractEntity entity, int depth)
    {
        var variables = entity.InstanceVariables.Where(p => !IsEncoding(p.Key)).OrderBy(p => Name(p.Key), StringComparer.Ordinal).ToList();
        Line($"iv:{variables.Count}");
        foreach (var (key, value) in variables)
        {
            Line("k:" + Name(key));
            Walk(value, depth + 1);
        }
    }

    private void Walk(AbstractEntity entity, int depth)
    {
        entity = entity.ResolveIfLink();
        MaxDepth = Math.Max(MaxDepth, depth);

        switch (entity)
        {
            case RubyNil: Line("nil"); return;
            case RubyTrue: Line("T"); return;
            case RubyFalse: Line("F"); return;
            case RubyFixNum number: Line("i:" + number.Value); return;
            case RubyBigNum big: Line("i:" + (BigInteger)big.Value); return;
            case RubyFloat real:
                var value = real.Value;
                Line(double.IsNaN(value) ? "f:nan"
                    : double.IsInfinity(value) ? "f:" + (value > 0 ? "+" : "-") + "inf"
                    : "f:" + BitConverter.DoubleToUInt64Bits(value).ToString("x16"));
                return;
            case RubySymbol symbol: Line("y:" + Hex(symbol.Value)); return;
        }

        if (_ids.TryGetValue(entity, out var id))
        {
            var seen = _visits[entity];
            _visits[entity] = (seen.Visits + 1, Math.Min(seen.Min, depth), Math.Max(seen.Max, depth));
            Line("@" + id);

            return;
        }

        _ids[entity] = _ids.Count;
        _visits[entity] = (1, depth, depth);

        switch (entity)
        {
            case RubyString text:
                Line($"s:{UserClass(text, "String")}:{Hex(text.Bytes)}");
                Modules(text);
                InstanceVariables(text, depth);
                break;
            case RubyArray array:
                Line($"a:{UserClass(array, "Array")}:{array.Elements.Count}");
                Modules(array);
                foreach (var element in array.Elements)
                    Walk(element, depth + 1);

                InstanceVariables(array, depth);
                break;
            case RubyHash hash:
                Line($"h:{UserClass(hash, "Hash")}:{hash.Pairs.Count}");
                Modules(hash);
                foreach (var pair in hash.Pairs)
                {
                    Walk(pair.Key, depth + 1);
                    Walk(pair.Value, depth + 1);
                }

                if (hash.Default == null)
                {
                    Line("d:none");
                }
                else
                {
                    Line("d:some");
                    Walk(hash.Default, depth + 1);
                }

                InstanceVariables(hash, depth);
                break;
            case RubyRegExp regexp:
                // Ruby drops the instance variables of a regexp when it loads it, so they are not described
                Line($"r:{UserClass(regexp, "Regexp")}:{Hex(regexp.Bytes)}:{(int)regexp.Options}");
                Modules(regexp);
                break;
            case RubyClass type: Line("c:" + Encoding.UTF8.GetString(type.Bytes)); break;
            case RubyModule module: Line("m:" + Encoding.UTF8.GetString(module.Bytes)); break;
            case RubyStruct structure:
                Line($"S:{Name(structure.Name)}:{structure.Fields.Count}");
                Modules(structure);
                foreach (var (key, member) in structure.Fields)
                {
                    Line("k:" + Name(key));
                    Walk(member, depth + 1);
                }

                InstanceVariables(structure, depth);
                break;
            case RubyUserMarshal marshal:
                Line("U:" + Name(marshal.ClassName));
                Modules(marshal);
                Walk(marshal.Object, depth + 1);
                break;
            case RubyUserDefined userDefined:
                Line($"u:{Name(userDefined.ClassName)}:{Hex(userDefined.Bytes)}");
                Modules(userDefined);
                break;
            case RubyObject obj:
                Line($"o:{Name(obj.ClassName)}:{obj.Fields.Count}");
                Modules(obj);
                foreach (var (key, field) in obj.Fields.OrderBy(p => Name(p.Key), StringComparer.Ordinal))
                {
                    Line("k:" + Name(key));
                    Walk(field, depth + 1);
                }

                break;
            default:
                throw new InvalidOperationException("Not described: " + entity.GetType().Name);
        }
    }
}
