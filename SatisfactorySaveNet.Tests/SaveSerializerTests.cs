using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;
using SatisfactorySaveNet.Abstracts.Maths.Vector;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SaveSerializerTests
{
    private readonly ISaveFileSerializer _serializer = SaveFileSerializer.Instance;

    [Test]
    public void Serialize_Then_Deserialize_Roundtrip()
    {
        var save = new SatisfactorySave
        {
            Header = new Header
            {
                HeaderVersion = 5,
                SaveVersion = 10,
                BuildVersion = 1,
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

        var data = _serializer.Serialize(save);
        var result = _serializer.Deserialize(data);

        result.Header.SaveVersion.Should().Be(save.Header.SaveVersion);
        result.Body.Should().BeOfType<BodyPreV8>();
    }

    [Test]
    public void Serialize_Then_Deserialize_Roundtrip_With_Content()
    {
        var component = new ComponentObject
        {
            TypePath = "SomeComponent",
            ObjectReference = new ObjectReference { LevelName = "Level", PathName = "Path" },
            ParentActorName = "Parent"
        };

        var collectable = new ObjectReference { LevelName = "CLevel", PathName = "CPath" };

        var actor = new ActorObject
        {
            TypePath = "SomeActor",
            ObjectReference = new ObjectReference { LevelName = "ALevel", PathName = "APath" },
            NeedTransform = 0,
            Rotation = new Vector4(0, 0, 0, 0),
            Position = new Vector3(0, 0, 0),
            Scale = new Vector3(1, 1, 1),
            PlacedInLevel = 1,
            ParentObjectRoot = string.Empty,
            ParentObjectName = string.Empty
        };

        var save = new SatisfactorySave
        {
            Header = new Header
            {
                HeaderVersion = 5,
                SaveVersion = 10,
                BuildVersion = 1,
                SaveName = "Test",
                MapName = "Map",
                MapOptions = string.Empty,
                SessionName = "Session",
                PlayedSeconds = 0,
                SaveDateTimeUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SessionVisibility = 0
            },
            Body = new BodyPreV8
            {
                Objects = new List<ComponentObject> { component, actor },
                Collectables = new List<ObjectReference> { collectable }
            }
        };

        var data = _serializer.Serialize(save);
        var result = _serializer.Deserialize(data);

        var body = result.Body.Should().BeOfType<BodyPreV8>().Subject;
        body.Objects.Should().HaveCount(2);
        body.Collectables.Should().HaveCount(1);

        var deserializedComponent = body.Objects.First(o => o is not ActorObject);
        deserializedComponent.Should().BeOfType<ComponentObject>().Which.ParentActorName.Should().Be("Parent");
        body.Objects.OfType<ActorObject>().Single().PlacedInLevel.Should().Be(1);
    }
}
