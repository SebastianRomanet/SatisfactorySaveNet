using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;
using FluentAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SaveSerializerTests
{
    private readonly ISaveFileSerializer _serializer = SaveFileSerializer.Instance;

    [TestCase(false)]
    [TestCase(true)]
    public async Task Serialize_Then_Deserialize_Roundtrip(bool async)
    {
        var save = new SatisfactorySave
        {
            Header = new Header
            {
                HeaderVersion = 5,
                SaveVersion = (int)FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents,
                BuildVersion = BuildVersions.Patch0613,
                SaveName = "Test",
                MapName = "Map",
                MapOptions = string.Empty,
                SessionName = "Session",
                PlayedSeconds = 0,
                SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SessionVisibility = 0
            },
            Body = new BodyPreV8(),
            DiscardedBytes = new byte[] { 1 }
        };

        SatisfactorySave result;
        if (async)
        {
            var data = await _serializer.SerializeAsync(save, CancellationToken.None);
            result = await _serializer.DeserializeAsync(data, CancellationToken.None);
        }
        else
        {
            var data = _serializer.Serialize(save);
            result = _serializer.Deserialize(data);
        }

        result.Header.SaveVersion.Should().Be(save.Header.SaveVersion);
        result.Body.Should().BeOfType<BodyPreV8>();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Serialize_Then_Deserialize_Roundtrip_Compressed(bool async)
    {
        var save = new SatisfactorySave
        {
            Header = new Header
            {
                HeaderVersion = 5,
                SaveVersion = 21,
                BuildVersion = BuildVersions.Patch0613,
                SaveName = "Test",
                MapName = "Map",
                MapOptions = string.Empty,
                SessionName = "Session",
                PlayedSeconds = 0,
                SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SessionVisibility = 0
            },
            Body = new BodyPreV8()
        };

        SatisfactorySave result;
        if (async)
        {
            var data = await _serializer.SerializeAsync(save, CancellationToken.None);
            result = await _serializer.DeserializeAsync(data, CancellationToken.None);
        }
        else
        {
            var data = _serializer.Serialize(save);
            result = _serializer.Deserialize(data);
        }

        result.Header.SaveVersion.Should().Be(save.Header.SaveVersion);
        result.Body.Should().BeOfType<BodyPreV8>();
    }

    [Test]
    public void Serialize_Then_Deserialize_Metadata_Roundtrip()
    {
        var save = new SatisfactorySave
        {
            Header = new Header
            {
                HeaderVersion = 5,
                SaveVersion = 21,
                BuildVersion = BuildVersions.Patch0613,
                SaveName = "Test",
                MapName = "Map",
                MapOptions = string.Empty,
                SessionName = "Session",
                PlayedSeconds = 0,
                SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SessionVisibility = 0
            },
            Body = new BodyPreV8(),
            ModelVersion = "1.2.3-test",
            DiscardedBytes = new byte[] { 1, 2, 3 }
        };

        var data = _serializer.Serialize(save);
        var result = _serializer.Deserialize(data);

        result.ModelVersion.Should().Be(save.ModelVersion);
        result.DiscardedBytes.Should().BeEquivalentTo(save.DiscardedBytes);
    }

    [Test]
    public void Serialize_Then_Deserialize_Metadata_Roundtrip_Compressed()
    {
        var save = new SatisfactorySave
        {
            Header = new Header
            {
                HeaderVersion = 5,
                SaveVersion = 21,
                BuildVersion = BuildVersions.Patch0613,
                SaveName = "Test",
                MapName = "Map",
                MapOptions = string.Empty,
                SessionName = "Session",
                PlayedSeconds = 0,
                SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SessionVisibility = 0
            },
            Body = new BodyPreV8(),
            ModelVersion = "1.2.3-test",
            DiscardedBytes = new byte[] { 1, 2, 3 }
        };

        var data = _serializer.Serialize(save);
        var result = _serializer.Deserialize(data);

        result.ModelVersion.Should().Be(save.ModelVersion);
        result.DiscardedBytes.Should().BeEquivalentTo(save.DiscardedBytes);
    }

    [Test]
    public void Constructor_With_Null_HeaderSerializer_Throws()
    {
        Action act = () => new SaveFileSerializer(null!, ChunkSerializer.Instance, BodySerializer.Instance);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("headerSerializer");
    }

    [Test]
    public void Constructor_With_Null_ChunkSerializer_Throws()
    {
        Action act = () => new SaveFileSerializer(HeaderSerializer.Instance, null!, BodySerializer.Instance);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("chunkSerializer");
    }

    [Test]
    public void Constructor_With_Null_BodySerializer_Throws()
    {
        Action act = () => new SaveFileSerializer(HeaderSerializer.Instance, ChunkSerializer.Instance, null!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("bodySerializer");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SerializeDeserialize_Large_File_Minimal_Memory(bool async)
    {
        var save = new SatisfactorySave
        {
            Header = new Header
            {
                HeaderVersion = 5,
                SaveVersion = 21,
                BuildVersion = BuildVersions.Patch0613,
                SaveName = "Test",
                MapName = "Map",
                MapOptions = string.Empty,
                SessionName = "Session",
                PlayedSeconds = 0,
                SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SessionVisibility = 0
            },
            Body = new BodyPreV8(),
            DiscardedBytes = new byte[5 * 1024 * 1024]
        };

        long before = GC.GetTotalMemory(true);
        if (async)
        {
            using var stream = new MemoryStream();
            await _serializer.SerializeAsync(save, stream, CancellationToken.None);
            stream.Position = 0;
            _ = await _serializer.DeserializeAsync(stream, CancellationToken.None);
        }
        else
        {
            using var stream = new MemoryStream();
            _serializer.Serialize(save, stream);
            stream.Position = 0;
            _ = _serializer.Deserialize(stream);
        }
        long after = GC.GetTotalMemory(true);
        (after - before).Should().BeLessThan(60 * 1024 * 1024);
    }

    [Test]
    public async Task SerializeAsync_CanceledToken_Throws()
    {
        var save = new SatisfactorySave
        {
            Header = new Header
            {
                HeaderVersion = 5,
                SaveVersion = 21,
                BuildVersion = BuildVersions.Patch0613,
                SaveName = "Test",
                MapName = "Map",
                MapOptions = string.Empty,
                SessionName = "Session",
                PlayedSeconds = 0,
                SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SessionVisibility = 0
            },
            Body = new BodyPreV8(),
            DiscardedBytes = new byte[5 * 1024 * 1024]
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();
        var temp = Path.GetTempFileName();
        var act = async () => await _serializer.SerializeAsync(save, temp, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        File.Delete(temp);
    }
}
