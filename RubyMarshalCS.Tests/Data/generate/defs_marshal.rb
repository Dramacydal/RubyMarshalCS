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
