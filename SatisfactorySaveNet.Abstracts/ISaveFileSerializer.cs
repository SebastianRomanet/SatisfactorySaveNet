using SatisfactorySaveNet.Abstracts.Model;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SatisfactorySaveNet.Abstracts;

public interface ISaveFileSerializer
{
    /// <summary>
    /// Deserializes a save game from the provided byte array.
    /// </summary>
    /// <param name="data">Binary save data.</param>
    /// <returns>The deserialized save.</returns>
    public SatisfactorySave Deserialize(byte[] data);

    /// <summary>
    /// Deserializes a save game from the provided <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <returns>The deserialized save.</returns>
    public SatisfactorySave Deserialize(Stream stream);

    /// <summary>
    /// Deserializes a save game located at the specified file path.
    /// </summary>
    /// <param name="path">Path to the save file.</param>
    /// <returns>The deserialized save.</returns>
    public SatisfactorySave Deserialize(string path);

    /// <summary>
    /// Asynchronously deserializes a save game from the provided byte array.
    /// </summary>
    /// <param name="data">Binary save data.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous deserialization operation.</returns>
    public Task<SatisfactorySave> DeserializeAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deserializes a save game from the provided <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous deserialization operation.</returns>
    public Task<SatisfactorySave> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deserializes a save game located at the specified file path.
    /// </summary>
    /// <param name="path">Path to the save file.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous deserialization operation.</returns>
    public Task<SatisfactorySave> DeserializeAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a save game to the provided <see cref="Stream"/>.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <param name="stream">Target stream.</param>
    void Serialize(SatisfactorySave save, Stream stream);

    /// <summary>
    /// Serializes a save game to the provided <see cref="Stream"/> asynchronously.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <param name="stream">Target stream.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous serialization operation.</returns>
    Task SerializeAsync(SatisfactorySave save, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a save game and writes it to the specified file path.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <param name="path">Destination file path.</param>
    void Serialize(SatisfactorySave save, string path);

    /// <summary>
    /// Serializes a save game and writes it to the specified file path asynchronously.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <param name="path">Destination file path.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous serialization operation.</returns>
    Task SerializeAsync(SatisfactorySave save, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a save game and returns the resulting byte array.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <returns>Binary representation of the save game.</returns>
    byte[] Serialize(SatisfactorySave save);

    /// <summary>
    /// Asynchronously serializes a save game and returns the resulting byte array.
    /// </summary>
    /// <param name="save">The save structure to serialize.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous serialization operation, producing the save data.</returns>
    Task<byte[]> SerializeAsync(SatisfactorySave save, CancellationToken cancellationToken = default);
}

