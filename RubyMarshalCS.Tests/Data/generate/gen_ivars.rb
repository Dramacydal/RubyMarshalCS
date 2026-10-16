# values that carry instance variables, in every position a reader can meet them: alone, before another value, nested
module Mod; end
class MyString < String; end
class MyArray < Array; end
class MyHash < Hash; end
class MyRegexp < Regexp; end
class Holder; def initialize(*v); v.each_with_index { |x, i| instance_variable_set(:"@v#{i}", x) }; end; end
Pt = Struct.new(:x, :y)
def iv(o, *pairs); pairs.each_slice(2) { |k, v| o.instance_variable_set(k, v) }; o; end

ms  = iv(MyString.new("bar"), :@z, 1)                       # a String subclass: its encoding is an ivar too
ms2 = iv(MyString.new("тест"), :@a, "x", :@b, [1, 2])
bin = iv(MyString.new("bin".b), :@only, 7)                  # no encoding ivar, one of its own
arr = iv([1, 2, 3], :@x, 1)                                 # a plain Array with an ivar
arr2 = iv([[1], "s", :sym], :@x, "text", :@y, nil)
marr = iv(MyArray.new([1, 2]), :@m, 5)                      # an Array subclass with an ivar
hash = iv({1 => 2, :a => "b"}, :@h, 1)
mhash = iv(MyHash.new, :@mh, 2); mhash[1] = 2
hdef = iv(Hash.new(5), :@hd, 3); hdef[1] = 2
st = iv(Pt.new(1, 2), :@s, 1)
re = iv(Regexp.new("ab.c", Regexp::IGNORECASE), :@r, 1) rescue nil
mre = (iv(MyRegexp.new("x"), :@mr, 1) rescue nil)
ext_arr = iv([1, 2], :@e, 1).extend(Mod)                    # extended and with an ivar
ext_ms = iv(MyString.new("e"), :@e, 1).extend(Mod)
obj = Holder.new(ms, arr, marr, hash)                       # all of them inside an object

cases = {
  "uclass_string_alone" => ms, "uclass_string_non_ascii" => ms2, "uclass_binary_string" => bin,
  "uclass_string_then_int" => [ms, 42], "uclass_string_nested" => [[ms], {1 => ms}, 42],
  "array_ivar_alone" => arr, "array_ivar_then_int" => [arr, 42], "array_ivar_rich" => arr2, "array_ivar_nested" => [[arr2], {k: arr}, 7],
  "uclass_array_ivar" => marr, "uclass_array_ivar_then_int" => [marr, 42],
  "hash_ivar" => hash, "hash_ivar_then_int" => [hash, 42], "uclass_hash_ivar" => mhash, "uclass_hash_ivar_then_int" => [mhash, 42],
  "hash_default_ivar" => hdef, "hash_default_ivar_then_int" => [hdef, 42],
  "struct_ivar" => st, "struct_ivar_then_int" => [st, 42],
  "regexp_ivar" => [re, 42], "uclass_regexp_ivar" => [mre, 42],
  "extended_array_ivar" => [ext_arr, 42], "extended_uclass_string_ivar" => [ext_ms, 42],
  "object_with_all" => [obj, 42],
  "shared_ivar_values" => (s = ms; [s, iv([1], :@a, s), iv([2], :@b, s), s, 42]),
  "cyclic_array_ivar" => (c = [1]; iv(c, :@me, c); [c, 42]),
  "many_in_a_row" => [ms, arr, marr, hash, mhash, hdef, st, ms2, arr2, 42],
}.compact
cases.each { |n, v| File.binwrite("#{n}.bin", Marshal.dump(v)) }
puts "#{cases.size} files"
