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
    public void BodyDeserializer_V51_WithNonPersistentLevel()
    {
        var header = new Header { SaveVersion = 51, MapName = "Map" };

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            // Minimal grid
            writer.Write(1); // partition count
            StringSerializer.Instance.Serialize(writer, string.Empty);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0);
            StringSerializer.Instance.Serialize(writer, string.Empty);
            writer.Write(0u);

            // One non-persistent level
            writer.Write(1); // nrLevels

            StringSerializer.Instance.Serialize(writer, "L1"); // level name
            writer.Write(28L); // binary length
            writer.Write(0); // nrObjectHeaders
            writer.Write(0); // nrCollectables
            writer.Write(4L); // binarySizeObjects
            writer.Write(0); // nrObjects
            writer.Write(0u); // unknown
            writer.Write(0); // nrSecondCollectables
            writer.Write(0L); // skip
            writer.Write(51); // saveVersion for level

            // Persistent level (implicit name)
            writer.Write(24L); // binary length
            writer.Write(0); // nrObjectHeaders
            writer.Write(0); // nrCollectables
            writer.Write(4L); // binarySizeObjects
            writer.Write(0); // nrObjects
            writer.Write(0); // nrSecondCollectables
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var body = BodySerializer.Instance.Deserialize(reader, header) as BodyV8;

        body.Should().NotBeNull();
        body!.Levels.Should().HaveCount(2);
    }

    [Test]
    [Ignore("Compression for save version 51 not supported")]
    public void SaveSerializer_V51_Roundtrip()
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
            Body = new BodyV8
            {
                Levels = { new Level() }
            }
        };

        var data = SaveFileSerializer.Instance.Serialize(save);
        var result = SaveFileSerializer.Instance.Deserialize(data);

        result.Header.SaveVersion.Should().Be(save.Header.SaveVersion);
    }
}

