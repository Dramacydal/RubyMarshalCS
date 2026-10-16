using RubyMarshalCS;
using RubyMarshalCS.Entities;
using RubyMarshalCS.Enums;
using RubyMarshalCS.Helper;
using RubyMarshalCS.Settings;
using Xunit;

namespace RubyMarshalCS.Tests;

/// <summary>
/// RubyMarshalCS against Ruby. The files in <c>Data/dumps</c> were made by Ruby (<c>Data/generate</c>), and
/// <c>Data/expected.txt</c> has the description of the values that Ruby itself made of them (see <see cref="RubyMarshalDescription"/>).
/// The files in <c>Data/golden</c> are what this library writes from what it reads; Ruby read each of them and got the
/// same value as from the original (checked when they were made, with <c>Marshal.dump(Marshal.load(a)) == Marshal.dump(Marshal.load(b))</c>).
/// </summary>
public sealed class RubyMarshalTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "Data");

    /// <summary>Name, digest and the number of lines of the description, for each file.</summary>
    public static IEnumerable<object[]> Dumps() =>
        File.ReadAllLines(Path.Combine(Folder, "expected.txt")).Select(line =>
        {
            var parts = line.Split(' ');

            return new object[] { parts[0], parts[1], int.Parse(parts[2]["nodes=".Length..]) };
        });

    private static byte[] Dump(string name) => File.ReadAllBytes(Path.Combine(Folder, "dumps", name + ".bin"));

    private static byte[] Golden(string name) => File.ReadAllBytes(Path.Combine(Folder, "golden", name + ".out.bin"));

    private static AbstractEntity Read(byte[] bytes, bool resolveLinks = true) =>
        RubyMarshal.Load(bytes, new DeserializationSettings { ResolveLinks = resolveLinks, EnsureReadToEnd = true });

    [Fact]
    public void ThereAreFilesToCheck()
    {
        Assert.True(Dumps().Count() >= 80);
        Assert.All(Dumps(), row => Assert.True(File.Exists(Path.Combine(Folder, "golden", row[0] + ".out.bin")), (string)row[0]));
    }

    [Theory]
    [MemberData(nameof(Dumps))]
    public void TheReaderHoldsWhatRubyHas(string name, string digest, int nodes) => BigStack.Run(() =>
    {
        var description = RubyMarshalDescription.Of(Read(Dump(name)));

        Assert.Equal(nodes, description.Nodes);
        Assert.Equal(digest, description.Digest);
    });

    [Theory]
    [MemberData(nameof(Dumps))]
    public void LinksKeptAsEntitiesOrResolvedAreTheSameValue(string name, string digest, int nodes) => BigStack.Run(() =>
    {
        var description = RubyMarshalDescription.Of(Read(Dump(name), resolveLinks: false));

        Assert.Equal(nodes, description.Nodes);
        Assert.Equal(digest, description.Digest);
    });

    [Theory]
    [MemberData(nameof(Dumps))]
    public void TheWriterGivesTheBytesThatRubyReadAsTheSameValue(string name, string digest, int nodes) => BigStack.Run(() =>
    {
        var written = RubyMarshal.Dump(Read(Dump(name)));

        Assert.True(written.AsSpan().SequenceEqual(Golden(name)), $"{name}: the written bytes are not the golden ones");

        // and the golden bytes hold the value that Ruby has
        var description = RubyMarshalDescription.Of(Read(written));
        Assert.Equal(nodes, description.Nodes);
        Assert.Equal(digest, description.Digest);
    });

    // ---- what the generated files are made for, in plain words ----

    [Fact]
    public void ManyObjectsAreMetAtDifferentLevelsOfNesting() => BigStack.Run(() =>
    {
        var shared = 0;
        var atDifferentDepths = 0;
        var deepest = 0;
        foreach (var row in Dumps().Where(r => ((string)r[0]).StartsWith("stress_")))
        {
            var description = RubyMarshalDescription.Of(Read(Dump((string)row[0])));
            shared += description.Sharing.Shared;
            atDifferentDepths += description.Sharing.AtDifferentDepths;
            deepest = Math.Max(deepest, description.MaxDepth);
        }

        // a check that the data holds what it is for: the same objects at several levels, not just a lot of plain values
        Assert.True(shared > 500, $"shared objects: {shared}");
        Assert.True(atDifferentDepths > 400, $"shared at different depths: {atDifferentDepths}");
        Assert.True(deepest > 20, $"deepest: {deepest}");
    });

    [Fact]
    public void TheDeepestChainsAreRead() => BigStack.Run(() =>
    {
        Assert.True(RubyMarshalDescription.Of(Read(Dump("deep_array_1500"))).MaxDepth >= 1500);
        Assert.True(RubyMarshalDescription.Of(Read(Dump("deep_objects_800"))).MaxDepth >= 800);
    });

    [Fact]
    public void AnObjectThatIsMetSeveralTimesIsOneEntity()
    {
        // ["shared string", "shared string", ["shared string"], {"shared string" => "shared string"}]
        var root = (RubyArray)Read(Dump("shared_refs"));
        var first = root.Elements[0];

        Assert.Same(first, root.Elements[1]);
        Assert.Same(first, ((RubyArray)root.Elements[2]).Elements[0]);
        Assert.Same(first, ((RubyHash)root.Elements[3]).Pairs[0].Value);
    }

    [Fact]
    public void ARingOfObjectsIsARing() => BigStack.Run(() =>
    {
        // the first object and a chain of 500 more, and the last one refers to the first: 501 steps lead back
        var first = (RubyObject)Read(Dump("ring_500"));
        var current = first;
        for (var i = 0; i < 501; i++)
            current = (RubyObject)current.Fields.Single(p => p.Key.ToString() == "@nxt").Value;

        Assert.Same(first, current);
    });

    [Fact]
    public void AStringOfAUserClassKeepsItsInstanceVariablesAndTheNextValueIsRead()
    {
        // [MyString.new("bar") with @z = 1, 42]; the encoding of the string is an instance variable too
        var root = (RubyArray)Read(Dump("uclass_string_then_int"));
        var text = (RubyString)root.Elements[0];

        Assert.Equal("MyString", text.UserClass!.ToString());
        Assert.Equal(["E", "@z"], text.InstanceVariables.Select(p => p.Key.ToString()).ToArray());
        Assert.Equal(1, ((RubyFixNum)text.InstanceVariables[1].Value).Value);
        Assert.Equal(42, ((RubyFixNum)root.Elements[1]).Value);
    }

    [Fact]
    public void AnArrayKeepsItsInstanceVariables()
    {
        // [[1, 2, 3] with @x = 1, 42]
        var root = (RubyArray)Read(Dump("array_ivar_then_int"));
        var array = (RubyArray)root.Elements[0];

        Assert.Equal(3, array.Elements.Count);
        Assert.Equal("@x", Assert.Single(array.InstanceVariables).Key.ToString());
        Assert.Equal(42, ((RubyFixNum)root.Elements[1]).Value);
    }

    [Fact]
    public void AnArrayOfAUserClassKeepsItsInstanceVariables()
    {
        var root = (RubyArray)Read(Dump("uclass_array_ivar_then_int"));
        var array = (RubyArray)root.Elements[0];

        Assert.Equal("MyArray", array.UserClass!.ToString());
        Assert.Equal("@m", Assert.Single(array.InstanceVariables).Key.ToString());
        Assert.Equal(42, ((RubyFixNum)root.Elements[1]).Value);
    }

    [Fact]
    public void ModulesAndClassesAreWrittenAsTheKindTheyAre()
    {
        var root = (RubyArray)Read(Dump("class_module"));
        var module = root.Elements.OfType<RubyModule>().First();

        Assert.Equal(RubyCodes.Module, module.Code);
        Assert.Equal(RubyCodes.ModuleOld, new RubyModuleOld().Code);
        Assert.True(RubyMarshal.Dump(root).AsSpan().SequenceEqual(Golden("class_module")));
    }

    [Fact]
    public void ASubclassFollowedByAnotherValueDoesNotShiftTheStream()
    {
        // the case of the stream that was read wrong: "Unsupported entity: 7" for the number of instance variables taken for a value
        foreach (var name in new[] { "uclass_string_nested", "extended_uclass_string_ivar", "many_in_a_row", "object_with_all", "shared_ivar_values" })
            Assert.NotNull(Read(Dump(name)));
    }

    // ---- the two way dictionary of the contexts ----

    [Fact]
    public void TheWayBackOfAWriterWorksFromTheFirstValueOn()
    {
        // a writer asks "is it written already?" before it writes, and adds after: the dictionary has nothing read into it
        var dictionary = new TwoWayDictionary<int, string>();

        Assert.False(dictionary.TryGetValue("a", out _));
        dictionary.Add(0, "a");
        dictionary.Add(1, "b");
        Assert.True(dictionary.TryGetValue("a", out int indexOfA));
        Assert.Equal(0, indexOfA);
        Assert.False(dictionary.TryGetValue("c", out _));
        dictionary.Add(2, "c");
        Assert.True(dictionary.TryGetValue("c", out int indexOfC));
        Assert.Equal(2, indexOfC);
        Assert.True(dictionary.TryGetValue(1, out string? valueOfOne));
        Assert.Equal("b", valueOfOne);
    }

    [Fact]
    public void TheWayBackOfAReaderIsBuiltFromWhatWasRead()
    {
        var dictionary = new TwoWayDictionary<int, string>();
        for (var i = 0; i < 1000; i++)
            dictionary.Add(i, "v" + i);

        Assert.True(dictionary.TryGetValue("v999", out int index));
        Assert.Equal(999, index);
        dictionary.Add(1000, "late");
        Assert.True(dictionary.TryGetValue("late", out int late));
        Assert.Equal(1000, late);
    }

    [Fact]
    public void AValueThatIsReplacedDoesNotLeadBackAnyMore()
    {
        foreach (var askedBeforeTheChange in new[] { false, true })
        {
            var dictionary = new TwoWayDictionary<int, string>();
            dictionary.Add(1, "old");
            if (askedBeforeTheChange)
                Assert.True(dictionary.TryGetValue("old", out int _));

            dictionary.Add(1, "new");

            Assert.False(dictionary.TryGetValue("old", out int _), $"asked before: {askedBeforeTheChange}");
            Assert.True(dictionary.TryGetValue("new", out int index));
            Assert.Equal(1, index);
        }
    }

    [Fact]
    public void ClearingEmptiesBothWays()
    {
        var dictionary = new TwoWayDictionary<int, string>();
        dictionary.Add(1, "a");
        Assert.True(dictionary.TryGetValue("a", out int _));

        dictionary.Clear();

        Assert.False(dictionary.TryGetValue("a", out int _));
        Assert.False(dictionary.TryGetValue(1, out string? _));
        dictionary.Add(2, "a");
        Assert.True(dictionary.TryGetValue("a", out int again));
        Assert.Equal(2, again);
    }
}
