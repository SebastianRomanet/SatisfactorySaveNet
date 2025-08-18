using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts.Model;
using SatisfactorySaveNet.Abstracts.Model.Properties;

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
            BuildVersion = 1,
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
}
