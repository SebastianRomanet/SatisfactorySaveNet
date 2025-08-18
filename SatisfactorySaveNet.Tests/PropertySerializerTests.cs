using System;
using System.IO;
using FluentAssertions;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts.Model.Properties;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PropertySerializerTests
{
    private static void WriteString(BinaryWriter writer, string value) => StringSerializer.Instance.Serialize(writer, value);

    [Test]
    public void Deserialize_IntProperty()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            WriteString(writer, "MyInt");
            WriteString(writer, nameof(IntProperty));
            writer.Write(4);
            writer.Write(0);
            writer.Write((sbyte)0);
            writer.Write(42);
        }
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var property = PropertySerializer.Instance.DeserializeProperty(reader);
        property.Should().BeOfType<IntProperty>().Which.Value.Should().Be(42);
    }

    [Test]
    public void Deserialize_FloatProperty()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            WriteString(writer, "MyFloat");
            WriteString(writer, nameof(FloatProperty));
            writer.Write(4);
            writer.Write(0);
            writer.Write((sbyte)0);
            writer.Write(3.14f);
        }
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var property = PropertySerializer.Instance.DeserializeProperty(reader);
        property.Should().BeOfType<FloatProperty>().Which.Value.Should().Be(3.14f);
    }

    [Test]
    public void Deserialize_BoolProperty()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            WriteString(writer, "MyBool");
            WriteString(writer, nameof(BoolProperty));
            writer.Write(0);
            writer.Write(0);
            writer.Write((sbyte)1);
            writer.Write((sbyte)0);
        }
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var property = PropertySerializer.Instance.DeserializeProperty(reader);
        property.Should().BeOfType<BoolProperty>().Which.Value.Should().Be(1);
    }

    [Test]
    public void Deserialize_StrProperty()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            WriteString(writer, "MyStr");
            WriteString(writer, nameof(StrProperty));
            writer.Write(4);
            writer.Write(0);
            writer.Write((sbyte)0);
            WriteString(writer, "Hello");
        }
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var property = PropertySerializer.Instance.DeserializeProperty(reader);
        property.Should().BeOfType<StrProperty>().Which.Value.Should().Be("Hello");
    }

    [Test]
    public void Deserialize_UnknownProperty_Throws()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            WriteString(writer, "Prop");
            WriteString(writer, "UnknownProperty");
        }
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var act = () => PropertySerializer.Instance.DeserializeProperty(reader);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
