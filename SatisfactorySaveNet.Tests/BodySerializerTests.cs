using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;
using SatisfactorySaveNet.Abstracts.Model.Properties;
using SatisfactorySaveNet.Abstracts.Maths.Vector;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class BodySerializerTests
{
    [Test]
    public void Deserialize_NonEmpty_Body()
    {
        var header = new Header
        {
            HeaderVersion = 5,
            SaveVersion = 10,
            BuildVersion = BuildVersions.Patch0613,
            MapName = "Map",
            MapOptions = string.Empty,
            SessionName = "Session",
            PlayedSeconds = 0,
            SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SessionVisibility = 0
        };

        using var bodyStream = new MemoryStream();
        using (var writer = new BinaryWriter(bodyStream, System.Text.Encoding.UTF8, true))
        {
            // One object header
            writer.Write(1);
            writer.Write(ComponentObject.TypeID);
            StringSerializer.Instance.Serialize(writer, "/Game/Type");
            StringSerializer.Instance.Serialize(writer, "Level");
            StringSerializer.Instance.Serialize(writer, "Path");
            StringSerializer.Instance.Serialize(writer, "Parent");

            // One object
            writer.Write(1);

            using var objectData = new MemoryStream();
            using (var objWriter = new BinaryWriter(objectData, System.Text.Encoding.UTF8, true))
            {
                using var props = new MemoryStream();
                using (var propWriter = new BinaryWriter(props, System.Text.Encoding.UTF8, true))
                {
                    StringSerializer.Instance.Serialize(propWriter, "MyInt");
                    StringSerializer.Instance.Serialize(propWriter, nameof(IntProperty));
                    propWriter.Write(4); // binarySize (ignored)
                    propWriter.Write(0); // index
                    propWriter.Write((sbyte)0);
                    propWriter.Write(123);
                    StringSerializer.Instance.Serialize(propWriter, "None");
                }
                var propBytes = props.ToArray();
                objWriter.Write(propBytes.Length + 4); // binary size includes extra data
                objWriter.Write(propBytes);
                objWriter.Write(new byte[4]); // extra data placeholder
            }
            var objectBytes = objectData.ToArray();
            writer.Write(objectBytes);

            // No collectables
            writer.Write(0);
        }

        bodyStream.Position = 0;
        using var reader = new BinaryReader(bodyStream);
        var body = BodySerializer.Instance.Deserialize(reader, header) as BodyPreV8;

        body.Should().NotBeNull();
        body!.Objects.Should().HaveCount(1);
        var obj = body.Objects.First();
        obj.Properties.Should().HaveCount(1);
        obj.Properties.First().Should().BeOfType<IntProperty>().Which.Value.Should().Be(123);
    }

    [Test]
    public void SerializeV8_WithCollectablesAndReferences_RoundTrip()
    {
        var header = new Header
        {
            HeaderVersion = 7,
            SaveVersion = 41,
            BuildVersion = BuildVersions.Patch0613,
            MapName = "Map",
            MapOptions = string.Empty,
            SessionName = "Session",
            PlayedSeconds = 0,
            SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SessionVisibility = 0
        };

        var body = new BodyV8();
        var level = new Level();
        level.Collectables.Add(new ObjectReference { LevelName = "Level", PathName = "/Game/Collectable1" });
        level.SecondCollectables!.Add(new ObjectReference { LevelName = "Level", PathName = "/Game/Collectable2" });
        body.Levels.Add(level);
        body.ObjectReferences = new List<ObjectReference>
        {
            new ObjectReference { LevelName = "Level", PathName = "/Game/GlobalRef" }
        };

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            BodySerializer.Instance.Serialize(writer, header, body);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var roundTripped = BodySerializer.Instance.Deserialize(reader, header) as BodyV8;

        roundTripped.Should().NotBeNull();
        roundTripped!.Levels.Single().Collectables.Should().HaveCount(1);
        roundTripped.Levels.Single().SecondCollectables.Should().HaveCount(1);
        roundTripped.ObjectReferences.Should().HaveCount(1);
        roundTripped.Levels.Single().Collectables.First().PathName.Should().Be("/Game/Collectable1");
        roundTripped.Levels.Single().SecondCollectables.First().PathName.Should().Be("/Game/Collectable2");
        roundTripped.ObjectReferences!.First().PathName.Should().Be("/Game/GlobalRef");
    }

    [Test]
    public void SerializeV8_WithObject_RoundTrip()
    {
        var header = new Header
        {
            HeaderVersion = 7,
            SaveVersion = 41,
            BuildVersion = BuildVersions.Patch0613,
            MapName = "Map",
            MapOptions = string.Empty,
            SessionName = "Session",
            PlayedSeconds = 0,
            SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SessionVisibility = 0
        };

        var actor = new ActorObject
        {
            TypePath = "/Game/Actor",
            ObjectReference = new ObjectReference { LevelName = "Level", PathName = "/Game/Actor" },
            NeedTransform = 1,
            Rotation = new Vector4(0, 0, 0, 1),
            Position = new Vector3(1, 2, 3),
            Scale = new Vector3(1, 1, 1),
            PlacedInLevel = 1,
            ParentObjectRoot = string.Empty,
            ParentObjectName = string.Empty
        };

        var level = new Level();
        level.Objects.Add(actor);
        var body = new BodyV8();
        body.Levels.Add(level);

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            BodySerializer.Instance.Serialize(writer, header, body);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var roundTripped = BodySerializer.Instance.Deserialize(reader, header) as BodyV8;

        roundTripped.Should().NotBeNull();
        roundTripped!.Levels.Single().Objects.Should().HaveCount(1);
        var rtActor = roundTripped.Levels.Single().Objects.First() as ActorObject;
        rtActor.Should().NotBeNull();
        rtActor!.TypePath.Should().Be("/Game/Actor");
        rtActor.Position.X.Should().Be(1f);
        rtActor.Position.Y.Should().Be(2f);
        rtActor.Position.Z.Should().Be(3f);
    }

    [Test]
    public void SerializeV8_WithCollectablesAndReferences_RoundTrip_SaveVersion51()
    {
        var header = new Header
        {
            HeaderVersion = 7,
            SaveVersion = 51,
            BuildVersion = BuildVersions.Patch0613,
            MapName = "Map",
            MapOptions = string.Empty,
            SessionName = "Session",
            PlayedSeconds = 0,
            SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SessionVisibility = 0
        };

        var actor = new ActorObject
        {
            TypePath = "/Game/Actor",
            ObjectReference = new ObjectReference { LevelName = "Level", PathName = "/Game/Actor" },
            NeedTransform = 1,
            Rotation = new Vector4(0, 0, 0, 1),
            Position = new Vector3(1, 2, 3),
            Scale = new Vector3(1, 1, 1),
            PlacedInLevel = 1,
            ParentObjectRoot = string.Empty,
            ParentObjectName = string.Empty
        };

        var level = new Level();
        level.Objects.Add(actor);
        level.Collectables.Add(new ObjectReference { LevelName = "Level", PathName = "/Game/Collectable1" });
        level.SecondCollectables!.Add(new ObjectReference { LevelName = "Level", PathName = "/Game/Collectable2" });
        var body = new BodyV8();
        body.Levels.Add(level);
        body.ObjectReferences = new List<ObjectReference>
        {
            new ObjectReference { LevelName = "Level", PathName = "/Game/GlobalRef" }
        };

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            BodySerializer.Instance.Serialize(writer, header, body);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var roundTripped = BodySerializer.Instance.Deserialize(reader, header) as BodyV8;

        roundTripped.Should().NotBeNull();
        var rtLevel = roundTripped!.Levels.Single();
        rtLevel.Objects.Should().HaveCount(1);
        rtLevel.Collectables.Should().HaveCount(1);
        rtLevel.SecondCollectables.Should().HaveCount(1);
        roundTripped.ObjectReferences.Should().HaveCount(1);

        var rtActor = rtLevel.Objects.First() as ActorObject;
        rtActor.Should().NotBeNull();
        rtActor!.TypePath.Should().Be("/Game/Actor");
        rtActor.Position.X.Should().Be(1f);
        rtActor.Position.Y.Should().Be(2f);
        rtActor.Position.Z.Should().Be(3f);
        rtLevel.Collectables.First().PathName.Should().Be("/Game/Collectable1");
        rtLevel.SecondCollectables.First().PathName.Should().Be("/Game/Collectable2");
        roundTripped.ObjectReferences!.First().PathName.Should().Be("/Game/GlobalRef");
    }
}
