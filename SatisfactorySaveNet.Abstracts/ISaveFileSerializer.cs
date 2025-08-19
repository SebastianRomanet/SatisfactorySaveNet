using SatisfactorySaveNet.Abstracts.Model;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SatisfactorySaveNet.Abstracts;

public interface ISaveFileSerializer
{
    public SatisfactorySave Deserialize(byte[] data);
    public SatisfactorySave Deserialize(Stream stream);
    public SatisfactorySave Deserialize(string path);

    public Task<SatisfactorySave> DeserializeAsync(byte[] data, CancellationToken cancellationToken = default);
    public Task<SatisfactorySave> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default);
    public Task<SatisfactorySave> DeserializeAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a save game to the provided <see cref="Stream"/>.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <param name="stream">Target stream.</param>
    void Serialize(SatisfactorySave save, Stream stream);

    Task SerializeAsync(SatisfactorySave save, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a save game and writes it to the specified file path.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <param name="path">Destination file path.</param>
    void Serialize(SatisfactorySave save, string path);

    Task SerializeAsync(SatisfactorySave save, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a save game and returns the resulting byte array.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <returns>Binary representation of the save game.</returns>
    byte[] Serialize(SatisfactorySave save);

    Task<byte[]> SerializeAsync(SatisfactorySave save, CancellationToken cancellationToken = default);
}

