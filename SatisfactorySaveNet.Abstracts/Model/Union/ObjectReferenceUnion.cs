namespace SatisfactorySaveNet.Abstracts.Model.Union;

public class ObjectReferenceUnion : UnionBase
{
    public override UnionConstraint Type => UnionConstraint.ObjectReference;
    public ObjectReference Value { get; set; } = new();
}
