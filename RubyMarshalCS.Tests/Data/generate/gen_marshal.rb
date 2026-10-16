# real Marshal dumps for every kind of value the format has
Point = Struct.new(:x, :y)
module Mod; end
module Mod2; end
class MyString < String; end
class MyArray < Array; end
class MyHash < Hash; end
class Plain; def initialize; @a = 1; @b = [1, 2, 3]; @s = "text"; end; end
class Cyclic; attr_accessor :me, :list; def initialize; @me = self; @list = [self]; end; end
class UserMarshal; def initialize(v); @v = v; end; def marshal_dump; [@v, "x"]; end; def marshal_load(a); @v = a[0]; end; end
class UserDefined; def initialize(s); @s = s; end; def _dump(l); @s; end; def self._load(s); new(s); end; end
class WithEncoding; def initialize; @sjis = "テスト".encode("Shift_JIS"); @bin = "\xFF\x00\xFE".b; @ascii = "abc".force_encoding("US-ASCII"); @u16 = "ab".encode("UTF-16LE"); end; end

shared = "shared string"
sym_list = [:a, :b, :a, :c, :b, :a]
o_ext = Object.new; o_ext.extend(Mod)
o_two = Object.new; o_two.extend(Mod); o_two.extend(Mod2)
str_ext = "ext"; str_ext.extend(Mod)
str_iv = "with ivar"; str_iv.instance_variable_set(:@note, "n")
s_sym = "sym".encode("UTF-8")
h_def = Hash.new(5); h_def[1] = 2; h_def[:k] = "v"
h_proc_free = {1 => {2 => {3 => [4, 5, {6 => 7}]}}}
mh = MyHash.new; mh[1] = 2
ms = MyString.new("bar"); ms.instance_variable_set(:@z, 1)
rng = Random.new(20260416)
many_floats = [1.0, 12.0, 120.0, 0.0001, 0.00009999, 1e15, 1e16, 1e17, 2.0**62, 5e-324, Float::MAX, Float::MIN, 0.1 + 0.2, -1e-7, 123456789.0, 4.35]
many_floats += Array.new(300) { (rng.rand - 0.5) * 10.0**rng.rand(-30..30) }
many_floats += Array.new(100) { rng.rand(1..10**rng.rand(1..20)).to_f }
many_floats += Array.new(100) { [rng.bytes(8).unpack1("G")].find { |f| f.finite? } || 1.5 }
cases = {
  "nil" => nil, "true" => true, "false" => false,
  "fixnums" => [0, 1, -1, 122, 123, -123, -124, 255, 256, 65535, 65536, 2**30 - 1, -(2**30), 2**31, -(2**31)],
  "bignums" => [2**40, -(2**40), 2**64, -(2**100), 2**200 + 12345],
  "floats_many" => many_floats,
  "floats" => [1.5, -0.0, 0.0, Float::INFINITY, -Float::INFINITY, Float::NAN, 1e100, 1.0e-100, 3.141592653589793],
  "symbols" => sym_list, "symbol_non_ascii" => [:"тест", :"日本語", :"a b"],
  "strings" => ["", "abc", "тест", "日本語", shared, shared, "a" * 1000],
  "encodings" => WithEncoding.new,
  "string_ivar" => str_iv, "string_extended" => str_ext,
  "regexp" => [/ab.c/i, /x/m, /y/x, /тест/, Regexp.new("a", Regexp::IGNORECASE | Regexp::MULTILINE)],
  "range" => [1..5, 1...5, "a".."z"],
  "struct" => Point.new(1, 2), "struct_array" => [Point.new(1, 2), Point.new(3, 4), Point.new("x", nil)],
  "class_module" => [Object, Kernel, String, Comparable, Point],
  "extended" => o_ext, "extended_two" => o_two,
  "uclass_string" => ms, "uclass_array" => MyArray.new([1, 2, 3]), "uclass_hash" => mh,
  "object" => Plain.new, "object_list" => [Plain.new, Plain.new, Plain.new],
  "object_cyclic" => Cyclic.new,
  "hash" => {1 => 2, :a => "b", "c" => [1, 2], nil => true}, "hash_default" => h_def, "hash_nested" => h_proc_free,
  "user_marshal" => UserMarshal.new(7), "user_marshal_list" => [UserMarshal.new(1), UserMarshal.new("s")],
  "user_defined" => UserDefined.new("raw bytes \x00\x01"), "time" => Time.at(1700000000, 123456, :usec).utc,
  "rational_complex" => [Rational(1, 3), Complex(1, 2)],
  "shared_refs" => [shared, shared, [shared], {shared => shared}],
  "mixed" => [Point.new(Plain.new, [shared, :sym, 1.5]), o_ext, ms, h_def, 2**70, /re/i, 1..3, UserMarshal.new([1, 2]), :sym, shared],
}
cases.each { |name, value| File.binwrite("#{name}.bin", Marshal.dump(value)) }
puts "#{cases.size} files, #{Dir['*.bin'].sum { |f| File.size(f) }} bytes"
