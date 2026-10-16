module Mod0; end
module Mod1; end
class UArr < Array; end
class UHash < Hash; end
class UStr < String; end
class UM; def initialize(a, b); @a = a; @b = b; end; def marshal_dump; [@a, @b]; end; def marshal_load(x); @a, @b = x; end; end
CLASSES = (0..9).map { |i| Object.const_set("Foo#{i}", Class.new) }
STRUCTS = (0..4).map { |i| Object.const_set("St#{i}", Struct.new(*(0..i + 1).map { |k| :"f#{k}" })) }
class Node; attr_accessor :nxt, :name; end
