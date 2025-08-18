using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;
using FluentAssertions;
using System;

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
}
