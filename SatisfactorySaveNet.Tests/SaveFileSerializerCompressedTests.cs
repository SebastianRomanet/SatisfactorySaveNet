using System;
using System.IO;
using System.IO.Compression;
using FluentAssertions;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Exceptions;
using SatisfactorySaveNet.Abstracts.Model;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SaveFileSerializerCompressedTests
{
    private static byte[] CreateCompressedSave(bool corrupt = false)
    {
        var header = new Header
        {
            HeaderVersion = 5,
            SaveVersion = 21,
            BuildVersion = BuildVersions.Patch0613,
            MapName = "Map",
            MapOptions = string.Empty,
            SessionName = "Session",
            PlayedSeconds = 0,
            SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SessionVisibility = 0
        };

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);

        HeaderSerializer.Instance.Serialize(writer, header);

        using var body = new MemoryStream();
        using (var bw = new BinaryWriter(body, System.Text.Encoding.UTF8, true))
        {
            bw.Write(0); // nrObjectHeaders
            bw.Write(0); // nrObjects
            bw.Write(0); // nrCollectables
        }
        var bodyBytes = body.ToArray();

        using var uncompressed = new MemoryStream();
        using (var bw = new BinaryWriter(uncompressed, System.Text.Encoding.UTF8, true))
        {
            bw.Write(bodyBytes.Length);
            bw.Write(bodyBytes);
        }
        var uncompressedBytes = uncompressed.ToArray();

        using var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed, CompressionLevel.Optimal, true))
        {
            z.Write(uncompressedBytes, 0, uncompressedBytes.Length);
        }
        var compressedBytes = compressed.ToArray();

        // Chunk header
        writer.Write(ChunkInfo.MagicValue);
        writer.Write(0);
        writer.Write(ChunkInfo.ChunkSize);
        writer.Write(0);

        // Summary chunk
        writer.Write(compressedBytes.Length);
        writer.Write(0);
        writer.Write(uncompressedBytes.Length);
        writer.Write(0);

        // Sub chunk (allow corruption)
        writer.Write(compressedBytes.Length);
        writer.Write(0);
        writer.Write(corrupt ? uncompressedBytes.Length + 1 : uncompressedBytes.Length);
        writer.Write(0);

        writer.Write(compressedBytes);

        return stream.ToArray();
    }

    [Test]
    public void Deserialize_Compressed_Save()
    {
        var data = CreateCompressedSave();
        var save = SaveFileSerializer.Instance.Deserialize(data);
        save.Header.SaveVersion.Should().Be(21);
        save.Body.Should().BeOfType<BodyPreV8>();
    }

    [Test]
    public void Deserialize_Corrupted_Chunk_Throws()
    {
        var data = CreateCompressedSave(corrupt: true);
        var act = () => SaveFileSerializer.Instance.Deserialize(data);
        act.Should().Throw<CorruptedSatisFactorySaveFileException>();
    }
}
