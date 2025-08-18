using System;
using System.IO;
using FluentAssertions;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class HeaderSerializerTests
{
    [Test]
    public void Deserialize_Throws_ForTooNewHeaderVersion()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write((int)SaveHeaderVersion.VersionPlusOne);
            writer.Write((int)FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents);
            writer.Write(0);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);

        Action act = () => HeaderSerializer.Instance.Deserialize(reader);
        act.Should().Throw<NotSupportedException>().WithMessage("*header version*");
    }

    [Test]
    public void Deserialize_Throws_ForUnsupportedSaveVersion()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write((int)SaveHeaderVersion.VersionPlusOne - 1);
            writer.Write((int)FSaveCustomVersion.VersionPlusOne);
            writer.Write(0);
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);

        Action act = () => HeaderSerializer.Instance.Deserialize(reader);
        act.Should().Throw<NotSupportedException>().WithMessage("*save version*");
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
}
