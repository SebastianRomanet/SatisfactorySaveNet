using FluentAssertions;
using NUnit.Framework;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;
using SatisfactorySaveNet.Abstracts.Model.Properties;
using SatisfactorySaveNet.Abstracts.Model.Union;
using System.IO;
using System.Linq;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PropertySerializerMapTests
{
    private readonly IPropertySerializer _serializer = PropertySerializer.Instance;
    private readonly Header _header = new();

    [Test]
    public void Deserialize_MapProperty_WithStringValue()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        StringSerializer.Instance.Serialize(writer, "MyMap");
        StringSerializer.Instance.Serialize(writer, nameof(MapProperty));
        writer.Write(0); // binarySize
        writer.Write(0); // index
        StringSerializer.Instance.Serialize(writer, nameof(IntProperty)); // key type
        StringSerializer.Instance.Serialize(writer, nameof(StrProperty)); // value type
        writer.Write((sbyte)0); // padding
        writer.Write(0); // modeType
        writer.Write(1); // count
        writer.Write(123); // key
        StringSerializer.Instance.Serialize(writer, "Hello"); // value
        ms.Position = 0;

        var property = (MapProperty)_serializer.DeserializeProperty(new BinaryReader(ms), _header)!;
        var element = property.Elements.Single();
        element.Key.Should().BeOfType<IntUnion>().Which.Value.Should().Be(123);
        element.Value.Should().BeOfType<StrUnion>().Which.Value.Should().Be("Hello");
    }
}
