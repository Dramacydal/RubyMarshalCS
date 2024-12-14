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
    
    public MethodInfo? OnPreSerializeMethod { get; set; }
    
    public MethodInfo? OnSerializeMethod { get; set; }
    
    public MethodInfo? OnPreDeserializeMethod { get; set; }
    
    public MethodInfo? OnDeserializeMethod { get; set; }
}
