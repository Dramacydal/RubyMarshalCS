using System.Reflection;

namespace RubyMarshalCS.Serialization;

public class TypeCandidateInfo
{
    public TypeCandidateInfo(Type type)
    {
        Type = type;
    }

    public Type Type { get; }

    public readonly Dictionary<string, Candidate> FieldCandidates = new();

    public Candidate? ExtensionDataCandidate { get; set; }
    
    public MethodInfo? OnSerializingMethod { get; set; }
    
    public MethodInfo? OnSerializedMethod { get; set; }
    
    public MethodInfo? OnDeserializingMethod { get; set; }
    
    public MethodInfo? OnDeserializedMethod { get; set; }
}
