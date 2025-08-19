using System;
using System.IO;
using System.IO.Compression;
using FluentAssertions;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Exceptions;
using SatisfactorySaveNet.Abstracts.Model;
using System.Threading.Tasks;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SaveFileSerializerCompressedTests
{
    private static byte[] CreateCompressedSave(bool corrupt = false, int extraSize = 0)
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
            if (extraSize > 0)
                bw.Write(new byte[extraSize]);
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

    [TestCase(false)]
    [TestCase(true)]
    public async Task Deserialize_Compressed_Save(bool async)
    {
        var data = CreateCompressedSave();
        SatisfactorySave save;
        if (async)
            save = await SaveFileSerializer.Instance.DeserializeAsync(data);
        else
            save = SaveFileSerializer.Instance.Deserialize(data);
        save.Header.SaveVersion.Should().Be(21);
        save.Body.Should().BeOfType<BodyPreV8>();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Deserialize_Corrupted_Chunk_Throws(bool async)
    {
        var data = CreateCompressedSave(corrupt: true);
        if (async)
        {
            var act = async () => await SaveFileSerializer.Instance.DeserializeAsync(data);
            await act.Should().ThrowAsync<CorruptedSatisFactorySaveFileException>();
        }
        else
        {
            var act = () => SaveFileSerializer.Instance.Deserialize(data);
            act.Should().Throw<CorruptedSatisFactorySaveFileException>();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Deserialize_Compressed_Large_File_Minimal_Memory(bool async)
    {
        var data = CreateCompressedSave(extraSize: 5 * 1024 * 1024);
        long before = GC.GetTotalMemory(true);
        if (async)
            _ = await SaveFileSerializer.Instance.DeserializeAsync(data);
        else
            _ = SaveFileSerializer.Instance.Deserialize(data);
        long after = GC.GetTotalMemory(true);
        (after - before).Should().BeLessThan(60 * 1024 * 1024);
    }
}
