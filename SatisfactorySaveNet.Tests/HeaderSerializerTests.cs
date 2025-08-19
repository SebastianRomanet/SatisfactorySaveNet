using System;
using System.IO;
using FluentAssertions;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;
using SatisfactorySaveNet;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class HeaderSerializerTests
{
    [Test]
    public void Deserialize_SetsFlag_ForTooNewHeaderVersion()
    {
        var header = new Header
        {
            HeaderVersion = (int)SaveHeaderVersion.VersionPlusOne,
            SaveVersion = (int)FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents,
            BuildVersion = BuildVersions.Patch1000,
            SaveName = string.Empty,
            MapName = string.Empty,
            MapOptions = string.Empty,
            SessionName = string.Empty,
            PlayedSeconds = 0,
            SaveDateTimeUtc = DateTime.UnixEpoch,
            SessionVisibility = 0,
            EditorObjectVersion = 0,
            ModMetadata = string.Empty,
            IsModdedSave = 0,
            SaveIdentifier = string.Empty,
            IsPartitionedWorld = 0,
            SaveDataHash = new string('0', 20),
            IsCreativeModeEnabled = 0
        };

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            HeaderSerializer.Instance.Serialize(writer, header);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var result = HeaderSerializer.Instance.Deserialize(reader);

        result.IsDeprecated.Should().BeTrue();
    }

    [Test]
    public void Deserialize_SetsFlag_ForUnsupportedSaveVersion()
    {
        var header = new Header
        {
            HeaderVersion = (int)SaveHeaderVersion.VersionPlusOne - 1,
            SaveVersion = (int)FSaveCustomVersion.VersionPlusOne,
            BuildVersion = BuildVersions.Patch1000,
            SaveName = string.Empty,
            MapName = string.Empty,
            MapOptions = string.Empty,
            SessionName = string.Empty,
            PlayedSeconds = 0,
            SaveDateTimeUtc = DateTime.UnixEpoch,
            SessionVisibility = 0,
            EditorObjectVersion = 0,
            ModMetadata = string.Empty,
            IsModdedSave = 0,
            SaveIdentifier = string.Empty,
            IsPartitionedWorld = 0,
            SaveDataHash = new string('0', 20),
            IsCreativeModeEnabled = 0
        };

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            HeaderSerializer.Instance.Serialize(writer, header);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var result = HeaderSerializer.Instance.Deserialize(reader);

        result.IsDeprecated.Should().BeTrue();
    }

    [Test]
    public void Deserialize_Throws_ForUnsupportedBuildVersion()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write((int)SaveHeaderVersion.InitialVersion);
            writer.Write((int)FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents);
            writer.Write(123); // unknown build version
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);

        Action act = () => HeaderSerializer.Instance.Deserialize(reader);
        act.Should().Throw<NotSupportedException>().WithMessage("*build version*");
    }

    [Test]
    public void Deserialize_DoesNotThrow_ForKnownBuildVersion()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write((int)SaveHeaderVersion.InitialVersion);
            writer.Write((int)FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents);
            writer.Write(BuildVersions.Patch1000);
            StringSerializer.Instance.Serialize(writer, string.Empty);
            StringSerializer.Instance.Serialize(writer, string.Empty);
            StringSerializer.Instance.Serialize(writer, string.Empty);
            writer.Write(0);
            writer.Write(DateTime.UnixEpoch.Ticks);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        Action act = () => HeaderSerializer.Instance.Deserialize(reader);
        act.Should().NotThrow();
    }
}
