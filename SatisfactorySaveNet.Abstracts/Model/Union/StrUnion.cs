namespace SatisfactorySaveNet.Abstracts.Model.Union;

public class StrUnion : UnionBase
{
    public override UnionConstraint Type => UnionConstraint.Str;
    public string Value { get; set; } = string.Empty;
}
