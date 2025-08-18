namespace SatisfactorySaveNet.Abstracts.Extra;

public class CargoObject
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Raw hex-encoded payload associated with the cargo object.
    /// </summary>
    public string SerializedData { get; set; } = string.Empty;
}
