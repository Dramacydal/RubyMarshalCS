# RubyMarshalCS

A C# library for reading and writing Ruby's [Marshal](https://ruby-doc.org/core/Marshal.html) binary format. Supports full bidirectional conversion between C# objects and Ruby Marshal data, including all primitive types, collections, objects, and custom serialization.

For RPG Maker VX / VX Ace examples see [RPGMaker.md](RPGMaker.md).

**[Wiki](https://github.com/Dramacydal/RubyMarshalCS/wiki)**

---

## Features

- Deserialize Ruby Marshal binary data into strongly-typed C# objects
- Serialize C# objects back into valid Ruby Marshal binary data
- Attribute-based mapping between C# classes and Ruby object types
- Custom serializers for `UserDefined` Ruby objects (e.g. `Table`, `Color`, `Tone`)
- Custom converters for full control over serialization/deserialization
- Configurable handling of missing members, unknown object types, and object links
- Support for all Ruby Marshal types: nil, bool, int, bignum, float, string, symbol, array, hash, object, struct, regex, and more

---

## Installation

Add the project reference or copy the `RubyMarshalCS` source into your solution. The only external dependency is [ZLibDotNet](https://www.nuget.org/packages/ZLibDotNet) for zlib compression.

---

## Quick Start

### Deserialize from file

```csharp
// Load as raw entity tree
AbstractEntity root = RubyMarshal.Load("Actors.rvdata2");

// Load and deserialize directly to a C# type
List<Actor> actors = RubyMarshal.Load<List<Actor>>("Actors.rvdata2");
```

### Deserialize from bytes or stream

```csharp
byte[] bytes = File.ReadAllBytes("data.rxdata");
var obj = RubyMarshal.Load<MyClass>(bytes);

using var stream = File.OpenRead("data.rxdata");
var obj = RubyMarshal.Load<MyClass>(stream);
```

### Two-step deserialization

```csharp
AbstractEntity root = RubyMarshal.Load("data.rxdata");
MyClass obj = RubyDeserializer.Deserialize<MyClass>(root, settings);
```

### Serialize to file

```csharp
List<Actor> actors = GetActors();
RubyMarshal.Dump("Actors.rvdata2", actors);
```

### Serialize to bytes

```csharp
byte[] bytes = RubyMarshal.Dump(myObject);
```

---

## Mapping C# Classes to Ruby Objects

Use `[RubyObject]` to register a C# class as a Ruby object type, and `[RubyProperty]` to map fields.

```csharp
[RubyObject("RPG::Actor")]
public class Actor
{
    [RubyProperty("@id")]
    public int Id;

    [RubyProperty("@name")]
    public string Name;

    [RubyProperty("@class_id")]
    public int ClassId;

    [RubyProperty("@initial_level")]
    public int InitialLevel;

    [RubyProperty("@features")]
    public List<Feature> Features;
}
```

The string passed to `[RubyObject]` must match the Ruby class name exactly (e.g. `"RPG::Actor"`). The string passed to `[RubyProperty]` must match the Ruby instance variable name including the `@` prefix.

### Auto-registration

By default, `SerializationHelper.AutoRegister` is `true`. All types decorated with `[RubyObject]` in loaded assemblies are registered automatically. You can also register manually:

```csharp
SerializationHelper.RegisterRubyObject(typeof(Actor));
```

---

## Context Tags

If you need multiple independent type registries (e.g. different Ruby formats), use context tags:

```csharp
[RubyObject("RPG::Actor", "myContext")]
public class Actor { ... }

var settings = new DeserializationSettings { ContextTag = "myContext" };
var actors = RubyMarshal.Load<List<Actor>>("Actors.rvdata2", settings);
```

---

## Lifecycle Callbacks

You can hook into the serialization lifecycle with attributes:

```csharp
[RubyObject("RPG::Map")]
public class Map
{
    [RubyProperty("@width")]
    public int Width;

    [RubyProperty("@height")]
    public int Height;

    [RubyOnDeserializing]
    public void OnDeserializing(RubyObject rubyObject)
    {
        // called before fields are set
    }

    [RubyOnDeserialized]
    public void OnDeserialized(RubyObject rubyObject)
    {
        // called after all fields are set
    }

    [RubyOnSerializing]
    public void OnSerializing(RubyObject rubyObject)
    {
        // called before serialization
    }

    [RubyOnSerialized]
    public void OnSerialized(RubyObject rubyObject)
    {
        // called after serialization
    }
}
```

---

## Extension Data

Capture unmapped Ruby fields into a dictionary instead of throwing an error:

```csharp
[RubyObject("RPG::Actor")]
public class Actor
{
    [RubyProperty("@id")]
    public int Id;

    [RubyExtensionData]
    public Dictionary<string, object?> ExtraFields { get; set; }
}
```

---

## Custom User Serializers

Ruby `UserDefined` objects (those that implement `_dump`/`_load` in Ruby) store raw binary data. Implement `IRubyUserSerializer<T>` to handle them:

```csharp
[RubyObject("Table")]
[RubyUserSerializer(typeof(TableSerializer))]
public class Table
{
    public int NumOfDimensions;
    public int SizeX, SizeY, SizeZ;
    public int NumOfElements;
    public List<short> Elements;
}

public class TableSerializer : IRubyUserSerializer<Table>
{
    public void Read(Table obj, BinaryReader reader)
    {
        obj.NumOfDimensions = reader.ReadInt32();
        obj.SizeX = reader.ReadInt32();
        obj.SizeY = reader.ReadInt32();
        obj.SizeZ = reader.ReadInt32();
        obj.NumOfElements = reader.ReadInt32();
        obj.Elements = new List<short>();
        for (int i = 0; i < obj.NumOfElements; i++)
            obj.Elements.Add(reader.ReadInt16());
    }

    public void Write(Table obj, BinaryWriter writer)
    {
        writer.Write(obj.NumOfDimensions);
        writer.Write(obj.SizeX);
        writer.Write(obj.SizeY);
        writer.Write(obj.SizeZ);
        writer.Write(obj.NumOfElements);
        foreach (var e in obj.Elements)
            writer.Write(e);
    }
}
```

---

## Custom Converters

For full control over how a type is serialized/deserialized, implement `IRubyCustomConverter` and register it with `[RubyCustomConverter]`:

```csharp
public class MyConverter : IRubyCustomConverter
{
    public bool CanConvert(Type type) => type == typeof(MyType);

    public AbstractEntity Serialize(object value, RubySerializer serializer)
    {
        // build and return an AbstractEntity
    }

    public object Deserialize(AbstractEntity value, RubyDeserializer deserializer)
    {
        // convert AbstractEntity to MyType
    }

    public object? Cast(object? o) => o;
}
```

---

## Deserialization Settings

```csharp
var settings = new DeserializationSettings
{
    // Context tag for type registry isolation (default: "")
    ContextTag = "",

    // Resolve object/symbol links immediately during reading (default: false)
    ResolveLinks = false,

    // How to handle Ruby fields with no matching C# property:
    //   Error (default), Store (into ExtensionData), Ignore
    MissingMemberHandling = MissingMemberHandling.Error,

    // Called when a field has no matching C# property
    MissingMember = (sender, args) => { ... },

    // How to handle Ruby objects with no registered C# type:
    //   Error (default), Ignore
    UndefinedObjectHandling = UndefinedObjectHandling.Error,

    // How to handle UserDefined objects with no registered serializer:
    //   Error (default), Store (as GenericUserObject), Ignore
    UndefinedUserObjectHandling = UndefinedUserObjectHandling.Error,

    // Throw if bytes remain unread after deserialization (default: false)
    EnsureReadToEnd = false,
};
```

---

## Serialization Settings

```csharp
var settings = new SerializationSettings
{
    // Context tag for type registry isolation (default: "")
    ContextTag = "",

    // Filter properties by In/Out CandidateFlags (default: true)
    ConsiderInOutFields = true,
};
```

---

## Working with the Raw Entity Tree

If you need to inspect or manipulate the Marshal data without mapping to C# types:

```csharp
AbstractEntity root = RubyMarshal.Load("data.rxdata");

if (root is RubyArray array)
{
    foreach (var element in array.Elements)
        Console.WriteLine(element.Code);
}

if (root is RubyHash hash)
{
    foreach (var (key, value) in hash.Pairs)
        Console.WriteLine($"{key} => {value}");
}

if (root is RubyObject obj)
{
    var name = obj.ClassName; // e.g. "RPG::Actor"
    foreach (var (field, value) in obj.Fields)
        Console.WriteLine($"{field} = {value}");
}
```

### Entity types

| Entity class | Ruby type | Notes |
|---|---|---|
| `RubyNil` | `nil` | |
| `RubyTrue` / `RubyFalse` | `true` / `false` | |
| `RubyFixNum` | `Integer` | 32-bit signed |
| `RubyBigNum` | `Bignum` | Arbitrary precision |
| `RubyFloat` | `Float` | Double precision |
| `RubyString` | `String` | Stored as `byte[]` |
| `RubySymbol` | `Symbol` | |
| `RubyArray` | `Array` | `List<AbstractEntity>` |
| `RubyHash` | `Hash` | Key-value pairs, optional default |
| `RubyObject` | Object instance | Named Ruby class |
| `RubyStruct` | `Struct` | Named fields |
| `RubyUserDefined` | `_dump`/`_load` | Raw binary payload |
| `RubyUserMarshal` | `marshal_dump`/`marshal_load` | Marshaled object payload |
| `RubyRegExp` | `Regexp` | |
| `RubyObjectLink` | Object reference | Resolved from cache |
| `RubySymbolLink` | Symbol reference | Resolved from cache |

---

## Special Types

### `BinaryString`

Wraps a `byte[]` with encoding awareness. Ruby strings are encoding-tagged and may not be valid UTF-8.

```csharp
var bs = new BinaryString(bytes, Encoding.UTF8);
string value = bs.Value;        // decoded string
byte[] raw = bs.Bytes;          // raw bytes
bs.Reencode(Encoding.Latin1);   // re-encode bytes
```

`BinaryString` implicitly converts to/from `string`.

### `DefDictionary<K, V>`

A dictionary with a Ruby default value (from `Hash.new(default)`):

```csharp
var dict = new DefDictionary<int, string>();
dict[1] = "one";
dict.DefaultValue = "unknown";
```

### `GenericUserObject`

Fallback for `UserDefined` objects with no registered serializer (when `UndefinedUserObjectHandling = Store`):

```csharp
var obj = new GenericUserObject();
obj.Name   // Ruby class name
obj.Data   // raw byte[]
```

---

## Compressed Data

Use `RubyDeflate` to compress/decompress zlib-compressed binary payloads stored in `UserDefined` objects:

```csharp
byte[] compressed = ((BinaryString)scriptArray[2]).Bytes;
string code = Encoding.UTF8.GetString(RubyDeflate.Inflate(compressed));

byte[] recompressed = RubyDeflate.Deflate(Encoding.UTF8.GetBytes(code));
```

---

## API Reference

### `RubyMarshal` — main entry point

**Deserialization**
```csharp
AbstractEntity Load(string path, DeserializationSettings? settings = null)
AbstractEntity Load(byte[] bytes, DeserializationSettings? settings = null)
AbstractEntity Load(Stream stream, DeserializationSettings? settings = null)
AbstractEntity Load(BinaryReader reader, DeserializationSettings? settings = null)

T? Load<T>(string path, DeserializationSettings? settings = null)
T? Load<T>(byte[] bytes, DeserializationSettings? settings = null)
T? Load<T>(Stream stream, DeserializationSettings? settings = null)
T? Load<T>(BinaryReader reader, DeserializationSettings? settings = null)
```

**Serialization**
```csharp
void   Dump(string path, object? obj, SerializationSettings? settings = null)
void   Dump(Stream stream, object? obj, SerializationSettings? settings = null)
void   Dump(BinaryWriter writer, object? obj, SerializationSettings? settings = null)
byte[] Dump(object? obj, SerializationSettings? settings = null)

void   Dump(string path, AbstractEntity entity)
void   Dump(Stream stream, AbstractEntity entity)
void   Dump(BinaryWriter writer, AbstractEntity entity)
byte[] Dump(AbstractEntity entity)
```

### `RubyDeserializer` — standalone deserializer

```csharp
T? Deserialize<T>(AbstractEntity data)

// Static shortcut
T? RubyDeserializer.Deserialize<T>(AbstractEntity data, DeserializationSettings? settings = null)
```

### `RubySerializer` — standalone serializer

```csharp
AbstractEntity Serialize(object? value)

// Static shortcut
AbstractEntity RubySerializer.Serialize(object? value, SerializationSettings? settings = null)
```

### `SerializationHelper` — type registry

```csharp
static bool AutoRegister { get; set; }   // default: true
static void RegisterRubyObject(Type t)
static void RegisterUserObjectSerializer(Type serializer)
static Candidate? GetFieldCandidate(Type type, string fieldName)
static IRubyCustomConverter? GetCustomConverter(Type type)
```
