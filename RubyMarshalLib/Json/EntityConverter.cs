using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RubyMarshal.Entities;

namespace RubyMarshal.Json;

public class EntityConverter : JsonConverter
{
    private readonly SerializationContext _context;

    public EntityConverter()
    {
    }
    
    public EntityConverter(SerializationContext context)
    {
        _context = context;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        JObject jo = new JObject();
        jo.AddFirst(new JProperty("RawClassName", value.GetType().ToString()));

        var valueType = value.GetType();
        
        var properties = valueType.GetProperties();
        foreach (var prop in properties)
        {
            if (prop.CustomAttributes.Any(_ => _.AttributeType == typeof(JsonIgnoreAttribute)))
                continue;
            
            if (prop.CanRead)
            {
                object propValue = prop.GetValue(value);
                if (propValue != null)
                {
                    jo.Add(prop.Name, JToken.FromObject(propValue, serializer));
                }
            }
        }

        foreach (var field in valueType.GetFields())
        {
            if (field.CustomAttributes.Any(_ => _.AttributeType == typeof(JsonIgnoreAttribute)))
                continue;
            
            if (field.IsPublic)
            {
                object fieldValue = field.GetValue(value);
                if (fieldValue != null)
                {
                    jo.Add(field.Name, JToken.FromObject(fieldValue, serializer));
                }
            }
        }
        jo.WriteTo(writer);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        
        if (jo.Properties().Any(_ => _.Name == "RawClassName"))
        {
            var target = Activator.CreateInstance(Type.GetType(jo["RawClassName"].ToString()));

            if (target is AbstractEntity e)
                e.Context = _context;

            serializer.Populate(jo.CreateReader(), target);
            
            return target;
        }

        throw new Exception("???????");
    }
    
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsSubclassOf(typeof(AbstractEntity));
    }
}