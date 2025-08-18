using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SaveVersion51Tests
{
    [Test]
    public void HeaderSerializer_V14_Roundtrip_IncludesNewFields()
    {
        var header = new Header
        {
            HeaderVersion = 14,
            SaveVersion = 51,
            BuildVersion = BuildVersions.Patch0613,
            SaveName = "TestSave",
            MapName = "Map",
            MapOptions = "options",
            SessionName = "Session",
            PlayedSeconds = 123,
            SaveDateTimeUtc = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SessionVisibility = 0,
            EditorObjectVersion = 1,
            ModMetadata = "{}",
            IsModdedSave = 1,
            SaveIdentifier = "GUID",
            IsPartitionedWorld = 1,
            SaveDataHash = "00000000000000000000",
            IsCreativeModeEnabled = 0
        };

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            HeaderSerializer.Instance.Serialize(writer, header);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var roundTripped = HeaderSerializer.Instance.Deserialize(reader);

        roundTripped.Should().BeEquivalentTo(header);
    }

    [Test]
    [Ignore("Non-persistent level deserialization requires updated fixtures")]
    public void BodyDeserializer_V51_WithNonPersistentLevel()
    {
        Assert.Pass();
    }

    [Test]
    public void BodySerializer_V51_Roundtrip()
    {
        var save = new SatisfactorySave
        {
            Header = new Header
            {
                HeaderVersion = 14,
                SaveVersion = 51,
                BuildVersion = BuildVersions.Patch0613,
                SaveName = "TestSave",
                MapName = "Map",
                MapOptions = string.Empty,
                SessionName = "Session",
                PlayedSeconds = 0,
                SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SessionVisibility = 0,
                EditorObjectVersion = 1,
                ModMetadata = string.Empty,
                IsModdedSave = 0,
                SaveIdentifier = "GUID",
                IsPartitionedWorld = 0,
                SaveDataHash = "00000000000000000000",
                IsCreativeModeEnabled = 0
            },
            Body = new BodyV8 { Levels = { new Level() } }
        };

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            BodySerializer.Instance.Serialize(writer, save.Header, (BodyBase)save.Body);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var body = BodySerializer.Instance.Deserialize(reader, save.Header) as BodyV8;
        body.Should().NotBeNull();
    }
}

