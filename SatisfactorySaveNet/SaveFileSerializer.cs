using Microsoft.IO;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Exceptions;
using SatisfactorySaveNet.Abstracts.Model;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SatisfactorySaveNet;

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

    public SaveFileSerializer(IHeaderSerializer headerSerializer, IChunkSerializer chunkSerializer, IBodySerializer bodySerializer)
    {
        _headerSerializer = headerSerializer;
        _chunkSerializer = chunkSerializer;
        _bodySerializer = bodySerializer;
    }

    public SatisfactorySave Deserialize(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Deserialize(stream);
    }

    public SatisfactorySave Deserialize(byte[] data)
    {
        using var stream = Manager.GetStream(data);
        return Deserialize(stream);
    }

    public SatisfactorySave Deserialize(Stream stream)
    {
        if (stream.Length == 0)
            throw new CorruptedSatisFactorySaveFileException("Save file is empty");

        using var reader = new BinaryReader(stream);

        var header = _headerSerializer.Deserialize(reader);

        BodyBase? body;
        byte[]? metadataBytes = null;

        if (header.SaveVersion < 21)
        {
            body = _bodySerializer.Deserialize(reader, header);
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var remaining = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
                metadataBytes = reader.ReadBytes(remaining);
            }
        }
        else
        {
            using var buffer = Manager.GetStream();
            var uncompressedSize = 0L;

            while (stream.Position < stream.Length)
            {
                var chunkInfo = _chunkSerializer.Deserialize(reader); //0, 4, 8, 12

                if (chunkInfo.CompressedSize != ChunkInfo.MagicValue || chunkInfo.UncompressedSize != ChunkInfo.ChunkSize)
                    throw new CorruptedSatisFactorySaveFileException("Corrupted chunk was read");

                if (header.SaveVersion >= 41)
                    _ = reader.ReadByte();

                var summary = _chunkSerializer.Deserialize(reader);  //16, 20, 24, 28

                var subChunk = _chunkSerializer.Deserialize(reader); //32, 36, 40, 44

                if (subChunk.UncompressedSize != summary.UncompressedSize)
                    throw new CorruptedSatisFactorySaveFileException("Corrupted sub chunk was read");

                using var chunk = Manager.GetStream();
                chunk.Write(reader.ReadBytes(summary.CompressedSize));
                chunk.Seek(0, SeekOrigin.Begin);

                using (var zStream = new ZLibStream(chunk, CompressionMode.Decompress, true))
                {
                    zStream.CopyTo(buffer);
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

    public async Task<SatisfactorySave> DeserializeAsync(string path)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
        return await DeserializeAsync(stream).ConfigureAwait(false);
    }

    public async Task<SatisfactorySave> DeserializeAsync(byte[] data)
    {
        using var stream = Manager.GetStream(data);
        return await DeserializeAsync(stream).ConfigureAwait(false);
    }

    public async Task<SatisfactorySave> DeserializeAsync(Stream stream)
    {
        using var buffer = Manager.GetStream();
        await stream.CopyToAsync(buffer).ConfigureAwait(false);
        if (buffer.Length == 0)
            throw new CorruptedSatisFactorySaveFileException("Save file is empty");
        buffer.Position = 0;
        return Deserialize(buffer);
    }

    public void Serialize(SatisfactorySave save, string path)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        Serialize(save, stream);
    }

    public byte[] Serialize(SatisfactorySave save)
    {
        using var stream = Manager.GetStream();
        Serialize(save, stream);
        return stream.ToArray();
    }

    public void Serialize(SatisfactorySave save, Stream stream)
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
                writer.Write(meta);
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
                zStream.Write(chunkBuffer, 0, read);
            }

            var compressedSize = (int)compressed.Length;
            compressed.Position = 0;

            // chunk header
            writer.Write(ChunkInfo.MagicValue);
            writer.Write(0);
            writer.Write(ChunkInfo.ChunkSize);
            writer.Write(0);

            if (save.Header.SaveVersion >= 41)
                writer.Write((byte)0);

            // summary header
            if (save.Header.SaveVersion >= 41)
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

            compressed.CopyTo(stream);
        }
    }

    public async Task SerializeAsync(SatisfactorySave save, Stream stream)
    {
        using var buffer = Manager.GetStream();
        Serialize(save, buffer);
        buffer.Position = 0;
        await buffer.CopyToAsync(stream).ConfigureAwait(false);
    }

    public async Task SerializeAsync(SatisfactorySave save, string path)
    {
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await SerializeAsync(save, stream).ConfigureAwait(false);
    }

    public async Task<byte[]> SerializeAsync(SatisfactorySave save)
    {
        using var stream = Manager.GetStream();
        await SerializeAsync(save, stream).ConfigureAwait(false);
        return stream.ToArray();
    }
}