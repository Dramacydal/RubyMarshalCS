# random object graphs with a lot of shared strings, symbols and objects, cycles, deep nesting
module Mod0; end
module Mod1; end
class UArr < Array; end
class UHash < Hash; end
class UStr < String; end
class UM; def initialize(a, b); @a = a; @b = b; end; def marshal_dump; [@a, @b]; end; def marshal_load(x); @a, @b = x; end; end
CLASSES = (0..9).map { |i| Object.const_set("Foo#{i}", Class.new) }
STRUCTS = (0..4).map { |i| Object.const_set("St#{i}", Struct.new(*(0..i + 1).map { |k| :"f#{k}" })) }
SYMS = %i[a b c d e f g h @x @y name id list] + [:"тест", :"日本"]

def build(rng, limit, max_depth)
  strings = Array.new(10) { |i| (i.even? ? "str#{i}" : "строка#{i}") + "x" * rng.rand(0..5) }
  made = []
  count = 0
  leaf = lambda do
    case rng.rand(10)
    when 0 then nil
    when 1 then true
    when 2 then false
    when 3 then rng.rand(-70000..70000)
    when 4 then rng.rand * 1000
    when 5 then 2**rng.rand(31..90) * (rng.rand(2) == 0 ? 1 : -1)
    when 6 then SYMS.sample(random: rng)
    when 7 then strings.sample(random: rng)            # the same string object again: an object link
    when 8 then (made.empty? ? nil : made.sample(random: rng))   # any earlier container: a link, maybe a cycle
    else "s#{rng.rand(1000)}"
    end
  end
  gen = nil
  gen = lambda do |depth|
    count += 1
    return leaf.call if count > limit || depth > max_depth
    r = rng.rand
    return leaf.call if r < 0.25
    v =
      if r < 0.42 then a = []; made << a; rng.rand(0..6).times { a << gen.call(depth + 1) }; a
      elsif r < 0.52 then h = {}; made << h; rng.rand(0..4).times { h[[SYMS.sample(random: rng), rng.rand(100)].sample(random: rng)] = gen.call(depth + 1) }; h
      elsif r < 0.72 then o = CLASSES.sample(random: rng).new; made << o; rng.rand(0..5).times { o.instance_variable_set(:"@v#{rng.rand(8)}", gen.call(depth + 1)) }; (o.extend([Mod0, Mod1].sample(random: rng)) if rng.rand < 0.1); o
      elsif r < 0.80 then s = STRUCTS.sample(random: rng); st = s.new; made << st; s.members.each { |m| st[m] = gen.call(depth + 1) }; st
      elsif r < 0.85 then u = UM.new(gen.call(depth + 1), gen.call(depth + 1)); made << u; u
      elsif r < 0.89 then ua = UArr.new; made << ua; rng.rand(0..3).times { ua << gen.call(depth + 1) }; ua
      elsif r < 0.92 then uh = UHash.new; made << uh; rng.rand(0..2).times { uh[rng.rand(50)] = gen.call(depth + 1) }; uh
      elsif r < 0.95 then x = UStr.new("ascii#{rng.rand(100)}".b); made << x; x
      else s = strings.sample(random: rng).dup; made << s; s
      end
    v
  end
  root = []
  made << root
  rng.rand(3..12).times { root << gen.call(0) }
  # make some cycles on purpose
  made.sample(5, random: rng).each { |c| c << made.sample(random: rng) if c.is_a?(Array) }
  root
end

300.times do |seed|
  rng = Random.new(seed)
  data = build(rng, rng.rand(200..4000), rng.rand(4..40))
  File.binwrite("stress_#{format('%03d', seed)}.bin", Marshal.dump(data))
end

# deep chains
a = []; cur = a; 1500.times { n = []; cur << n; cur = n }; File.binwrite("deep_array_1500.bin", Marshal.dump(a))
class Node; attr_accessor :nxt, :name; end
first = Node.new; cur = first; 800.times { |i| n = Node.new; n.name = "n#{i % 7}"; cur.nxt = n; cur = n }; File.binwrite("deep_objects_800.bin", Marshal.dump(first))
ring = Node.new; cur = ring; 500.times { n = Node.new; n.name = :sym; cur.nxt = n; cur = n }; cur.nxt = ring; File.binwrite("ring_500.bin", Marshal.dump(ring))
shared = "s"; wide = Array.new(5000) { |i| [shared, :a, shared, :b, [shared, :a]] }; File.binwrite("wide_shared_5000.bin", Marshal.dump(wide))
sym_heavy = Array.new(3000) { |i| Hash[(0..7).map { |k| [SYMS[k], [SYMS[(k + i) % SYMS.size]]] }] }; File.binwrite("sym_heavy_3000.bin", Marshal.dump(sym_heavy))
puts "#{Dir['*.bin'].size} files, #{Dir['*.bin'].sum { |f| File.size(f) } / 1024} KB, biggest #{Dir['*.bin'].map { |f| File.size(f) }.max / 1024} KB"
