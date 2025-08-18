using SatisfactorySaveNet.Abstracts.Model;

namespace SatisfactorySaveNet.Abstracts.Model.Typed;

public class RailroadTrackPosition : TypedData
{
    public override TypedDataConstraint Type => TypedDataConstraint.RailroadTrackPosition;

    public ObjectReference Track { get; set; } = new();
    public float Offset { get; set; }
    public float Forward { get; set; }
}