GC.disable
# The same description of a value graph is written here (from the Ruby objects) and in canon/Program.cs (from the entities of the C# reader);
# when a hash of the description is equal, the entities hold exactly what Ruby has: the kinds, the bytes, the numbers, the order,
# the instance variables, and which objects are the same object (an object that is met again is written as @number of its first visit)
require 'digest'
class Canon
  attr_reader :nodes
  def initialize; @h = Digest::SHA256.new; @ids = {}.compare_by_identity; @nodes = 0; @text = ENV['CANON_TEXT'] ? +"" : nil; end
  def digest = @h.hexdigest[0, 12]
  def text = @text
  def e(s); @h << s << "\n"; @nodes += 1; @text << s << "\n" if @text; end
  def hex(s) = s.b.unpack1('H*')
  def cls(x, base) = (x.class.name == base ? "" : x.class.name)
  def mods(x)
    return if x.frozen?
    m = (x.singleton_class.ancestors - x.class.ancestors - [x.singleton_class]).map(&:name).compact.sort
    e("e:#{m.join(',')}") unless m.empty?
  end
  def ivars(x)
    names = x.instance_variables.sort
    e("iv:#{names.size}")
    names.each { |n| e("k:#{n}"); walk(x.instance_variable_get(n)) }
  end
  def walk(x)
    case x
    when nil then e("nil")
    when true then e("T")
    when false then e("F")
    when Integer then e("i:#{x}")
    when Float then e(x.nan? ? "f:nan" : x.infinite? ? "f:#{x > 0 ? '+' : '-'}inf" : "f:#{[x].pack('G').unpack1('H*')}")
    when Symbol then e("y:#{hex(x.to_s)}")
    else
      if @ids.key?(x) then e("@#{@ids[x]}"); return end
      @ids[x] = @ids.size
      case x
      when String then e("s:#{cls(x, 'String')}:#{hex(x)}"); mods(x); ivars(x)
      when Array then e("a:#{cls(x, 'Array')}:#{x.size}"); mods(x); x.each { |v| walk(v) }; ivars(x)
      when Hash
        e("h:#{cls(x, 'Hash')}:#{x.size}"); mods(x); x.each { |k, v| walk(k); walk(v) }
        x.default.nil? ? e("d:none") : (e("d:some"); walk(x.default)); ivars(x)
      when Regexp then e("r:#{cls(x, 'Regexp')}:#{hex(x.source)}:#{x.options}"); mods(x)   # Ruby drops the instance variables of a regexp when it loads it
      when Range then e("o:Range:3"); mods(x); e("k:begin"); walk(x.begin); e("k:end"); walk(x.end); e("k:excl"); walk(x.exclude_end?)
      when Class then e("c:#{x.name}")
      when Module then e("m:#{x.name}")
      when Struct then e("S:#{x.class.name}:#{x.members.size}"); mods(x); x.members.each { |m| e("k:#{m}"); walk(x[m]) }; ivars(x)
      else
        n = x.class.name
        if n == "UM" || n == "UserMarshal" then e("U:#{n}"); mods(x); walk(x.marshal_dump)
        elsif x.instance_variable_defined?(:@__raw) then e("u:#{n}:#{hex(x.instance_variable_get(:@__raw))}"); mods(x)
        elsif n == "UserDefined" then e("u:#{n}:#{hex(x.instance_variable_get(:@s))}"); mods(x)
        else
          names = x.instance_variables.sort
          e("o:#{n}:#{names.size}"); mods(x)
          names.each { |iv| e("k:#{iv}"); walk(x.instance_variable_get(iv)) }
        end
      end
    end
  end
end

load ARGV[0]
dir = ARGV[1]
ext = ENV["CANON_EXT"] || "bin"
Dir["#{dir}/*.#{ext}"].sort.each do |f|
  c = Canon.new
  begin
    c.walk(Marshal.load(File.binread(f)))
    File.write("#{ENV['CANON_TEXT']}/#{File.basename(f, ".#{ext}")}.ruby.txt", c.text) if ENV['CANON_TEXT']
    puts "#{File.basename(f, ".#{ext}").ljust(30)} #{c.digest} nodes=#{c.nodes}"
  rescue => ex
    puts "#{File.basename(f, ".#{ext}").ljust(30)} RUBY-FAILS #{ex.class} #{ex.message[0, 50]}"
  end
end
