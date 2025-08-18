using FluentAssertions;
using NUnit.Framework;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;
using SatisfactorySaveNet.Abstracts.Model.Properties;
using System.IO;
using System.Linq;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PropertySerializerArrayTests
{
    private readonly IPropertySerializer _serializer = PropertySerializer.Instance;
    private readonly Header _header = new();

    [Test]
    public void Deserialize_ArrayNameProperty()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(0); // binarySize
        writer.Write(0); // index
        StringSerializer.Instance.Serialize(writer, nameof(NameProperty));
        writer.Write((sbyte)0); // padding
        writer.Write(2); // count
        StringSerializer.Instance.Serialize(writer, "First");
        StringSerializer.Instance.Serialize(writer, "Second");
        ms.Position = 0;

        var property = (ArrayProperty)_serializer.DeserializeProperty(new BinaryReader(ms), _header, nameof(ArrayProperty))!;
        property.Property.Should().BeOfType<ArrayNameProperty>()
            .Which.Values.Should().Equal(new[] { "First", "Second" });
    }

    [Test]
    public void Deserialize_ArrayUInt32Property()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(0);
        writer.Write(0);
        StringSerializer.Instance.Serialize(writer, nameof(UInt32Property));
        writer.Write((sbyte)0);
        writer.Write(2);
        writer.Write((uint)1);
        writer.Write((uint)2);
        ms.Position = 0;

        var property = (ArrayProperty)_serializer.DeserializeProperty(new BinaryReader(ms), _header, nameof(ArrayProperty))!;
        property.Property.Should().BeOfType<ArrayUInt32Property>()
            .Which.Values.Should().Equal(new uint[] { 1u, 2u });
    }

    [Test]
    public void Deserialize_ArrayInt8Property()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(0);
        writer.Write(0);
        StringSerializer.Instance.Serialize(writer, nameof(Int8Property));
        writer.Write((sbyte)0);
        writer.Write(2);
        writer.Write((sbyte)1);
        writer.Write((sbyte)-2);
        ms.Position = 0;

        var property = (ArrayProperty)_serializer.DeserializeProperty(new BinaryReader(ms), _header, nameof(ArrayProperty))!;
        property.Property.Should().BeOfType<ArrayInt8Property>()
            .Which.Values.Should().Equal(new sbyte[] { 1, -2 });
    }

    [Test]
    public void Deserialize_ArrayFINNetworkProperty()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(0);
        writer.Write(0);
        StringSerializer.Instance.Serialize(writer, nameof(FINNetworkProperty));
        writer.Write((sbyte)0);
        writer.Write(1); // count
        // element
        StringSerializer.Instance.Serialize(writer, "Level");
        StringSerializer.Instance.Serialize(writer, "Path");
        writer.Write(0); // previous flag
        writer.Write(0); // step flag
        ms.Position = 0;

        var property = (ArrayProperty)_serializer.DeserializeProperty(new BinaryReader(ms), _header, nameof(ArrayProperty))!;
        var array = property.Property.Should().BeOfType<ArrayFINNetworkProperty>().Subject;
        array.Values.Should().ContainSingle();
        var element = array.Values.Single();
        element.ObjectReference.LevelName.Should().Be("Level");
        element.ObjectReference.PathName.Should().Be("Path");
        element.Previous.Should().BeNull();
        element.Step.Should().BeNull();
    }
}

