# Ruby Marshal files for the tests of RubyMarshalCS

`RubyMarshalTests` (run with `dotnet test RubyMarshalCS.Tests`) checks that RubyMarshalCS reads and writes Ruby Marshal data exactly as Ruby does. The tests do not need Ruby: the files and the expected results were made by Ruby once.

| What | Where | Made by |
|---|---|---|
| Data | `dumps/*.bin` | Ruby (`Marshal.dump`), by the scripts in `generate/` |
| What Ruby says the data holds | `expected.txt`: name, digest, number of lines | `generate/canon.rb`, which describes the objects that `Marshal.load` makes |
| What this library writes | `golden/*.out.bin` | RubyMarshalCS: `RubyMarshal.Dump(RubyMarshal.Load(dump))` with the links resolved |

The test project describes the entities of the reader in the same way (`RubyMarshalDescription`); equal digests mean that the reader holds what Ruby has: the kinds of values, the bytes, the numbers, the order, the instance variables, and which objects are the same object. Each golden file was read by Ruby and gave the same value as the original (`Marshal.dump(Marshal.load(a)) == Marshal.dump(Marshal.load(b))`, with `GC.disable`; Ruby keeps the frozen strings used as hash keys in a weak table, so with the collector on the answer for such strings can change from run to run).

The data:

- `stress_*`: random graphs of arrays, hashes, objects with instance variables, structs, objects extended by modules, user classes and user marshalled objects; the same strings, symbols and objects are met again and again at different levels of nesting, with cycles. `deep_array_1500`, `deep_objects_800`, `ring_500` and `wide_shared_5000` are the extreme cases.
- the files with the names of the kinds of values (`struct`, `bignums`, `floats`, `regexp`, `hash_default`, `user_marshal`, ...): one of each kind that the format has (`generate/gen_marshal.rb` makes more: `Time` and `Rational`/`Complex` are left out because the description cannot be made of their objects).
- `*_ivar*`, `uclass_*`: values with instance variables in every place a reader can meet them: strings, arrays, hashes, structs, regexps and the subclasses of them, alone, before another value, nested.

Making them again (Ruby 3.1 or later, the scripts write into the current folder):

```bash
cd RubyMarshalCS.Tests/Data/generate
mkdir out && cd out
ruby ../gen_stress.rb && ruby ../gen_marshal.rb && ruby ../gen_ivars.rb
ruby ../canon.rb ../defs_stress.rb .   # the description that Ruby makes: one line for each file
```

(`defs_*.rb` are the definitions of the classes that the files use, which `Marshal.load` needs; use the one that fits the files.) The golden files are made by the library, and then have to be read by Ruby again to confirm them.
