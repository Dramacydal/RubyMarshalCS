module Mod; end
class MyString < String; end
class MyArray < Array; end
class MyHash < Hash; end
class MyRegexp < Regexp; end
class Holder; def initialize(*v); v.each_with_index { |x, i| instance_variable_set(:"@v#{i}", x) }; end; end
Pt = Struct.new(:x, :y)
def iv(o, *pairs); pairs.each_slice(2) { |k, v| o.instance_variable_set(k, v) }; o; end
