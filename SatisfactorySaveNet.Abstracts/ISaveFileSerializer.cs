using SatisfactorySaveNet.Abstracts.Model;
using System.IO;

namespace SatisfactorySaveNet.Abstracts;

public interface ISaveFileSerializer
{
    public SatisfactorySave Deserialize(byte[] data);
    public SatisfactorySave Deserialize(Stream stream);
    public SatisfactorySave Deserialize(string path);

    /// <summary>
    /// Serializes a save game to the provided <see cref="Stream"/>.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <param name="stream">Target stream.</param>
    void Serialize(SatisfactorySave save, Stream stream);

    /// <summary>
    /// Serializes a save game and writes it to the specified file path.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <param name="path">Destination file path.</param>
    void Serialize(SatisfactorySave save, string path);

    /// <summary>
    /// Serializes a save game and returns the resulting byte array.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <returns>Binary representation of the save game.</returns>
    byte[] Serialize(SatisfactorySave save);
}