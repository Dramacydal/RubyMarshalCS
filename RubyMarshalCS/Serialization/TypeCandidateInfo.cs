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
}
