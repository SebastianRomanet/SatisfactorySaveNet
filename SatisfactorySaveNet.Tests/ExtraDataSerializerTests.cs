using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts.Extra;
using SatisfactorySaveNet.Abstracts.Model;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ExtraDataSerializerTests
{
    private const string VehiclePath = "/Game/FactoryGame/Buildable/Vehicle/Tractor/BP_Tractor.BP_Tractor_C";

    [TestCase(40, 53)]
    [TestCase(41, 105)]
    public void VehicleData_round_trips(int saveVersion, int payloadSize)
    {
        var header = new Header { SaveVersion = saveVersion, HeaderVersion = 0 };
        var count = 1;
        var name = "Cargo";
        var payload = new string('A', payloadSize);

        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
        {
            writer.Write(count);
            writer.Write(1); // element count
            StringSerializer.Instance.Serialize(writer, name);
            HexSerializer.Instance.Serialize(writer, payload);
        }
        var bytes = ms.ToArray();

        using var reader = new BinaryReader(new MemoryStream(bytes));
        var extra = ExtraDataSerializer.Instance.Deserialize(reader, VehiclePath, header, bytes.Length);

        extra.Should().BeOfType<VehicleData>();
        var vehicle = (VehicleData)extra!;
        vehicle.Count.Should().Be(count);
        vehicle.CargoObjects.Should().ContainSingle();
        var cargo = vehicle.CargoObjects.First();
        cargo.Name.Should().Be(name);
        cargo.SerializedData.Should().Be(payload);

        using var roundtrip = new MemoryStream();
        using (var writer = new BinaryWriter(roundtrip, Encoding.UTF8, true))
        {
            writer.Write(vehicle.Count);
            writer.Write(vehicle.CargoObjects.Count);
            StringSerializer.Instance.Serialize(writer, cargo.Name);
            HexSerializer.Instance.Serialize(writer, cargo.SerializedData);
        }

        roundtrip.ToArray().Should().Equal(bytes);
    }
}
