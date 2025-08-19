namespace SatisfactorySaveNet.Abstracts.Model;

/// <summary>
/// SatisfactorySave is the main class for parsing a savegame
/// </summary>
public class SatisfactorySave
{
    /// <summary>
    /// Header part of the save containing things like the version and metadata
    /// </summary>
    public required Header Header { get; set; }

    /// <summary>
    /// Body part of the save containing things like subLevels
    /// </summary>
    public required BodyBase? Body { get; set; }

    /// <summary>
    /// Any bytes that where not parsed by the serializer.
    /// These bytes will be written back when serializing again so that
    /// no information is lost when round-tripping a save file.
    /// </summary>
    public byte[]? DiscardedBytes { get; set; }

    /// <summary>
    /// Version of the <c>SatisfactorySaveNet</c> model that processed this save.
    /// This is stored in the metadata appended to the save file and is
    /// preserved across serialization round-trips.
    /// </summary>
    public string? ModelVersion { get; set; }
}