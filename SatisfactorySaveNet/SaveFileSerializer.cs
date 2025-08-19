using Microsoft.IO;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Exceptions;
using SatisfactorySaveNet.Abstracts.Model;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace SatisfactorySaveNet;

/// <summary>
/// Provides methods to serialize and deserialize <see cref="SatisfactorySave"/> instances.
/// </summary>
public class SaveFileSerializer : ISaveFileSerializer
{
    public static readonly ISaveFileSerializer Instance = new SaveFileSerializer(HeaderSerializer.Instance, ChunkSerializer.Instance, BodySerializer.Instance);

    private const int BlockSize = 256 * 1024;
    private const int LargeBufferMultiple = 64 * 1024 * 1024;
    private const int MaxBufferSize = 128 * 1024 * 1024;

    private static readonly RecyclableMemoryStreamManager Manager = new(new(BlockSize, LargeBufferMultiple, MaxBufferSize, 100 * BlockSize, MaxBufferSize * 4));//BlockSize, LargeBufferMultiple, MaxBufferSize);

    static SaveFileSerializer()
    {
#if DEBUG
        //Manager.GenerateCallStacks = true;
#endif
        //Manager.AggressiveBufferReturn = true;
        //Manager.MaximumFreeLargePoolBytes = MaxBufferSize * 4;
        //Manager.MaximumFreeSmallPoolBytes = 100 * BlockSize;
    }

    private readonly IHeaderSerializer _headerSerializer;
    private readonly IChunkSerializer _chunkSerializer;
    private readonly IBodySerializer _bodySerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveFileSerializer"/> class.
    /// </summary>
    /// <param name="headerSerializer">Serializer for the save file header.</param>
    /// <param name="chunkSerializer">Serializer for chunk metadata.</param>
    /// <param name="bodySerializer">Serializer for the save file body.</param>
    public SaveFileSerializer(IHeaderSerializer headerSerializer, IChunkSerializer chunkSerializer, IBodySerializer bodySerializer)
    {
        ArgumentNullException.ThrowIfNull(headerSerializer);
        ArgumentNullException.ThrowIfNull(chunkSerializer);
        ArgumentNullException.ThrowIfNull(bodySerializer);

        _headerSerializer = headerSerializer;
        _chunkSerializer = chunkSerializer;
        _bodySerializer = bodySerializer;
    }

    /// <summary>
    /// Deserializes a <see cref="SatisfactorySave"/> from the specified file path.
    /// </summary>
    /// <param name="path">The path to the save file.</param>
    /// <returns>The deserialized save.</returns>
    public SatisfactorySave Deserialize(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Deserialize(stream);
    }

    /// <summary>
    /// Deserializes a <see cref="SatisfactorySave"/> from the provided byte array.
    /// </summary>
    /// <param name="data">The raw save file data.</param>
    /// <returns>The deserialized save.</returns>
    public SatisfactorySave Deserialize(byte[] data)
    {
        using var stream = Manager.GetStream(data);
        return Deserialize(stream);
    }

    /// <summary>
    /// Deserializes a <see cref="SatisfactorySave"/> from the given stream.
    /// </summary>
    /// <param name="stream">The stream containing the save data.</param>
    /// <returns>The deserialized save.</returns>
    public SatisfactorySave Deserialize(Stream stream) => DeserializeInternal(stream, false, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously deserializes a <see cref="SatisfactorySave"/> from the specified file path.
    /// </summary>
    /// <param name="path">The path to the save file.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized save.</returns>
    public async Task<SatisfactorySave> DeserializeAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
        return await DeserializeAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously deserializes a <see cref="SatisfactorySave"/> from the provided byte array.
    /// </summary>
    /// <param name="data">The raw save file data.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized save.</returns>
    public async Task<SatisfactorySave> DeserializeAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        using var stream = Manager.GetStream(data);
        return await DeserializeAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously deserializes a <see cref="SatisfactorySave"/> from the given stream.
    /// </summary>
    /// <param name="stream">The stream containing the save data.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized save.</returns>
    public Task<SatisfactorySave> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default) => DeserializeInternal(stream, true, cancellationToken).AsTask();

    /// <summary>
    /// Serializes the specified <see cref="SatisfactorySave"/> to the given file path.
    /// </summary>
    /// <param name="save">The save to serialize.</param>
    /// <param name="path">The destination file path.</param>
    public void Serialize(SatisfactorySave save, string path)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        SerializeInternal(save, stream, false, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Serializes the specified <see cref="SatisfactorySave"/> and returns the result as a byte array.
    /// </summary>
    /// <param name="save">The save to serialize.</param>
    /// <returns>The serialized save data.</returns>
    public byte[] Serialize(SatisfactorySave save)
    {
        using var stream = Manager.GetStream();
        SerializeInternal(save, stream, false, CancellationToken.None).GetAwaiter().GetResult();
        return stream.ToArray();
    }

    /// <summary>
    /// Serializes the specified <see cref="SatisfactorySave"/> to the provided stream.
    /// </summary>
    /// <param name="save">The save to serialize.</param>
    /// <param name="stream">The destination stream.</param>
    public void Serialize(SatisfactorySave save, Stream stream) => SerializeInternal(save, stream, false, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously serializes the specified <see cref="SatisfactorySave"/> to the provided stream.
    /// </summary>
    /// <param name="save">The save to serialize.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SerializeAsync(SatisfactorySave save, Stream stream, CancellationToken cancellationToken = default) => SerializeInternal(save, stream, true, cancellationToken).AsTask();

    /// <summary>
    /// Asynchronously serializes the specified <see cref="SatisfactorySave"/> to the given file path.
    /// </summary>
    /// <param name="save">The save to serialize.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SerializeAsync(SatisfactorySave save, string path, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await SerializeInternal(save, stream, true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously serializes the specified <see cref="SatisfactorySave"/> and returns the result as a byte array.
    /// </summary>
    /// <param name="save">The save to serialize.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the serialized save data.</returns>
    public async Task<byte[]> SerializeAsync(SatisfactorySave save, CancellationToken cancellationToken = default)
    {
        using var stream = Manager.GetStream();
        await SerializeInternal(save, stream, true, cancellationToken).ConfigureAwait(false);
        return stream.ToArray();
    }

    private async ValueTask<SatisfactorySave> DeserializeInternal(Stream stream, bool async, CancellationToken cancellationToken)
    {
        if (stream.CanSeek && stream.Length == 0)
            throw new CorruptedSatisFactorySaveFileException("Save file is empty");

        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);

        var header = _headerSerializer.Deserialize(reader);

        BodyBase? body;
        byte[]? metadataBytes = null;

        if (header.SaveVersion < 21)
        {
            body = _bodySerializer.Deserialize(reader, header);
            if (stream.CanSeek && stream.Position < stream.Length)
            {
                var remaining = (int)(stream.Length - stream.Position);
                metadataBytes = await ReadRemainingAsync(stream, remaining, async, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            using var buffer = Manager.GetStream();
            var uncompressedSize = 0L;
            var infoBuffer = new byte[16];

            while (!stream.CanSeek || stream.Position < stream.Length)
            {
                var read = await ReadAsync(stream, infoBuffer, 0, infoBuffer.Length, async, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                if (read < infoBuffer.Length)
                    throw new CorruptedSatisFactorySaveFileException("Corrupted chunk was read");

                using var ms = new MemoryStream(infoBuffer, 0, infoBuffer.Length, false, true);
                using var br = new BinaryReader(ms);
                var chunkInfo = _chunkSerializer.Deserialize(br); //0,4,8,12

                if (chunkInfo.CompressedSize != ChunkInfo.MagicValue || chunkInfo.UncompressedSize != ChunkInfo.ChunkSize)
                    throw new CorruptedSatisFactorySaveFileException("Corrupted chunk was read");

                if (header.SaveVersion >= 41)
                    await ReadExactlyAsync(stream, infoBuffer, 1, async, cancellationToken).ConfigureAwait(false);

                await ReadExactlyAsync(stream, infoBuffer, infoBuffer.Length, async, cancellationToken).ConfigureAwait(false);
                using var ms2 = new MemoryStream(infoBuffer, 0, infoBuffer.Length, false, true);
                using var br2 = new BinaryReader(ms2);
                var summary = _chunkSerializer.Deserialize(br2);  //16,20,24,28

                await ReadExactlyAsync(stream, infoBuffer, infoBuffer.Length, async, cancellationToken).ConfigureAwait(false);
                using var ms3 = new MemoryStream(infoBuffer, 0, infoBuffer.Length, false, true);
                using var br3 = new BinaryReader(ms3);
                var subChunk = _chunkSerializer.Deserialize(br3); //32,36,40,44

                if (subChunk.UncompressedSize != summary.UncompressedSize)
                    throw new CorruptedSatisFactorySaveFileException("Corrupted sub chunk was read");

                using var chunk = Manager.GetStream();
                await CopyExactAsync(stream, chunk, summary.CompressedSize, async, cancellationToken).ConfigureAwait(false);
                chunk.Position = 0;

                using (var zStream = new ZLibStream(chunk, CompressionMode.Decompress, true))
                {
                    await CopyToAsync(zStream, buffer, async, cancellationToken).ConfigureAwait(false);
                }

                uncompressedSize += summary.UncompressedSize;
            }

            buffer.Position = 0;

            using var bufferReader = new BinaryReader(buffer);

            long dataLength;
            if (header.SaveVersion >= 41)
                dataLength = bufferReader.ReadInt64();
            else
                dataLength = bufferReader.ReadInt32();

            var offset = header.SaveVersion >= 41 ? 8 : 4;

            if (uncompressedSize < dataLength + offset)
                throw new CorruptedSatisFactorySaveFileException("Umcompressed size mismatch detected");

            body = _bodySerializer.Deserialize(bufferReader, header);

            if (bufferReader.BaseStream.Position < bufferReader.BaseStream.Length)
            {
                var remaining = (int)(bufferReader.BaseStream.Length - bufferReader.BaseStream.Position);
                metadataBytes = bufferReader.ReadBytes(remaining);
            }
        }

        var (modelVersion, discarded) = ParseMetadata(metadataBytes);

        return new SatisfactorySave
        {
            Header = header,
            Body = body,
            ModelVersion = modelVersion ?? GetAssemblyVersion(),
            DiscardedBytes = discarded
        };
    }

    private async ValueTask SerializeInternal(SatisfactorySave save, Stream stream, bool async, CancellationToken cancellationToken)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);

        _headerSerializer.Serialize(writer, save.Header);
        if (save.Body == null)
            throw new ArgumentNullException(nameof(save.Body));

        if (save.Header.SaveVersion < 21)
        {
            _bodySerializer.Serialize(writer, save.Header, save.Body);
            var meta = BuildMetadata(save);
            if (meta.Length > 0)
                await WriteAsync(stream, meta, 0, meta.Length, async, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var bodyStream = Manager.GetStream();
        using (var bodyWriter = new BinaryWriter(bodyStream, System.Text.Encoding.UTF8, true))
        {
            _bodySerializer.Serialize(bodyWriter, save.Header, save.Body);
            bodyWriter.Flush();
        }
        bodyStream.Position = 0;

        using var buffer = Manager.GetStream();
        if (save.Header.SaveVersion >= 41)
            buffer.Write(BitConverter.GetBytes(bodyStream.Length));
        else
            buffer.Write(BitConverter.GetBytes((int)bodyStream.Length));

        bodyStream.CopyTo(buffer);

        var metaData = BuildMetadata(save);
        if (metaData.Length > 0)
            buffer.Write(metaData, 0, metaData.Length);

        buffer.Position = 0;

        var chunkBuffer = new byte[ChunkInfo.ChunkSize];

        while (buffer.Position < buffer.Length)
        {
            var read = buffer.Read(chunkBuffer, 0, chunkBuffer.Length);

            using var compressed = Manager.GetStream();
            using (var zStream = new ZLibStream(compressed, CompressionMode.Compress, true))
            {
                await WriteAsync(zStream, chunkBuffer, 0, read, async, cancellationToken).ConfigureAwait(false);
            }

            var compressedSize = (int)compressed.Length;
            compressed.Position = 0;

            var headerBytes = BuildChunkHeader(save.Header, compressedSize, read);
            await WriteAsync(stream, headerBytes, 0, headerBytes.Length, async, cancellationToken).ConfigureAwait(false);

            await CopyToAsync(compressed, stream, async, cancellationToken).ConfigureAwait(false);
        }
    }

    private static byte[] BuildChunkHeader(Header header, int compressedSize, int read)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(ChunkInfo.MagicValue);
        writer.Write(0);
        writer.Write(ChunkInfo.ChunkSize);
        writer.Write(0);

        if (header.SaveVersion >= 41)
            writer.Write((byte)0);

        if (header.SaveVersion >= 41)
        {
            writer.Write((long)compressedSize);
            writer.Write(0L);
            writer.Write((long)read);
            writer.Write(0L);

            writer.Write((long)compressedSize);
            writer.Write(0L);
            writer.Write((long)read);
            writer.Write(0L);
        }
        else
        {
            writer.Write(compressedSize);
            writer.Write(0);
            writer.Write(read);
            writer.Write(0);

            writer.Write(compressedSize);
            writer.Write(0);
            writer.Write(read);
            writer.Write(0);
        }

        writer.Flush();
        return ms.ToArray();
    }

    private static ValueTask<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, bool async, CancellationToken cancellationToken) =>
        async ? stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken) : new ValueTask<int>(stream.Read(buffer, offset, count));

    private static ValueTask WriteAsync(Stream stream, byte[] buffer, int offset, int count, bool async, CancellationToken cancellationToken)
    {
        if (async)
            return stream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        stream.Write(buffer, offset, count);
        return ValueTask.CompletedTask;
    }

    private static ValueTask CopyToAsync(Stream source, Stream destination, bool async, CancellationToken cancellationToken)
    {
        if (async)
            return new ValueTask(source.CopyToAsync(destination, cancellationToken));
        source.CopyTo(destination);
        return ValueTask.CompletedTask;
    }

    private static async ValueTask ReadExactlyAsync(Stream stream, byte[] buffer, int count, bool async, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < count)
        {
            var read = await ReadAsync(stream, buffer, offset, count - offset, async, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
        }
    }

    private static async ValueTask CopyExactAsync(Stream source, Stream destination, int count, bool async, CancellationToken cancellationToken)
    {
        var temp = new byte[81920];
        var remaining = count;
        while (remaining > 0)
        {
            var toRead = Math.Min(temp.Length, remaining);
            var read = await ReadAsync(source, temp, 0, toRead, async, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException();
            await WriteAsync(destination, temp, 0, read, async, cancellationToken).ConfigureAwait(false);
            remaining -= read;
        }
    }

    private static async ValueTask<byte[]> ReadRemainingAsync(Stream stream, int count, bool async, CancellationToken cancellationToken)
    {
        var data = new byte[count];
        await ReadExactlyAsync(stream, data, count, async, cancellationToken).ConfigureAwait(false);
        return data;
    }

    private static byte[] BuildMetadata(SatisfactorySave save)
    {
        if (string.IsNullOrEmpty(save.ModelVersion) && (save.DiscardedBytes == null || save.DiscardedBytes.Length == 0))
            return Array.Empty<byte>();

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(save.ModelVersion ?? string.Empty);
        if (save.DiscardedBytes == null)
        {
            writer.Write(0);
        }
        else
        {
            writer.Write(save.DiscardedBytes.Length);
            writer.Write(save.DiscardedBytes);
        }

        return stream.ToArray();
    }

    private static (string? modelVersion, byte[]? discarded) ParseMetadata(byte[]? metadata)
    {
        if (metadata == null || metadata.Length == 0)
            return (null, null);

        using var stream = new MemoryStream(metadata);
        using var reader = new BinaryReader(stream);

        var version = reader.ReadString();
        var length = reader.ReadInt32();
        byte[]? discarded = length > 0 ? reader.ReadBytes(length) : null;

        return (string.IsNullOrEmpty(version) ? null : version, discarded);
    }

    private static string GetAssemblyVersion() => typeof(SaveFileSerializer).Assembly.GetName().Version?.ToString() ?? string.Empty;
}