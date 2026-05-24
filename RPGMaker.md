# RPG Maker VX / VX Ace

RubyMarshalCS can read and write RPG Maker VX (`.rxdata`) and VX Ace (`.rvdata2`) data files, which use Ruby's Marshal format.

---

## Example: Actors.rvdata2

Reading, modifying, and re-writing `Actors.rvdata2`:

```csharp
[RubyObject("RPG::Actor")]
public class Actor
{
    [RubyProperty("@id")]       public int Id;
    [RubyProperty("@name")]     public string Name;
    [RubyProperty("@note")]     public string Note;
    [RubyProperty("@features")] public List<Feature> Features;
}

[RubyObject("RPG::BaseItem::Feature")]
public class Feature
{
    [RubyProperty("@code")]     public int Code;
    [RubyProperty("@data_id")]  public int DataId;
    [RubyProperty("@value")]    public double Value;
}

// Read
var actors = RubyMarshal.Load<List<Actor>>("Actors.rvdata2");

// Modify
actors[1].Name = "MyHero";

// Write back
RubyMarshal.Dump("Actors.rvdata2", actors);
```

## Compressed Scripts

RPG Maker stores game scripts zlib-compressed inside `UserDefined` objects. Use `RubyDeflate` to compress/decompress them:

```csharp
byte[] compressed = ((BinaryString)scriptArray[2]).Bytes;
string code = Encoding.UTF8.GetString(RubyDeflate.Inflate(compressed));

byte[] recompressed = RubyDeflate.Deflate(Encoding.UTF8.GetBytes(code));
```
