using System.IO;
using System.Text;
using FluentAssertions;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Extra;
using SatisfactorySaveNet.Abstracts.Maths.Vector;
using SatisfactorySaveNet.Abstracts.Model;
using SatisfactorySaveNet.Abstracts.Model.Typed;
using System.Linq;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class InventoryItemSerializerTests
{
    [Test]
    public void DeserializeInventoryItem_with_fin_state_returns_stateful_item()
    {
        var header = new Header { SaveVersion = 44, HeaderVersion = 0 };
        var itemType = "/Game/FactoryGame/Equipment/Chainsaw/Desc_Chainsaw.Desc_Chainsaw_C";
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
        {
            writer.Write(0); // unknown
            StringSerializer.Instance.Serialize(writer, itemType);
            writer.Write(1); // state
            writer.Write(0); // unknown
            StringSerializer.Instance.Serialize(writer, "/Script/FicsItNetworksComputer.FINItemStateFileSystem");
            writer.Write(4); // length
            HexSerializer.Instance.Serialize(writer, "ABCD");
        }

        var bytes = ms.ToArray();
        using var reader = new BinaryReader(new MemoryStream(bytes));
        var result = TypedDataSerializer.Instance.Deserialize(reader, header, nameof(InventoryItem), false, bytes.Length);

        result.Should().BeOfType<StatefulInventoryItem>();
        var item = (StatefulInventoryItem)result;
        item.ItemType.Should().Be(itemType);
        item.FINItemStateFileSystem.Should().Be("ABCD");
    }

    [Test]
    public void DeserializeConveyor_with_fin_state_returns_stateful_item()
    {
        var header = new Header { SaveVersion = 44, HeaderVersion = 0 };
        var typePath = "/Game/FactoryGame/Buildable/Factory/ConveyorBeltMk1/Build_ConveyorBeltMk1.Build_ConveyorBeltMk1_C";
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
        {
            writer.Write(1); // count
            writer.Write(1); // number of elements
            // name ObjectReference
            StringSerializer.Instance.Serialize(writer, string.Empty);
            StringSerializer.Instance.Serialize(writer, "Item");
            writer.Write(1); // state
            // itemState ObjectReference
            StringSerializer.Instance.Serialize(writer, string.Empty);
            StringSerializer.Instance.Serialize(writer, "/Script/FicsItNetworksComputer.FINItemStateFileSystem");
            writer.Write(4); // length
            HexSerializer.Instance.Serialize(writer, "ABCD");
            // position
            writer.Write((sbyte)1);
            writer.Write((sbyte)2);
            writer.Write((sbyte)3);
            writer.Write((sbyte)4);
        }

        var bytes = ms.ToArray();
        using var reader = new BinaryReader(new MemoryStream(bytes));
        var extra = ExtraDataSerializer.Instance.Deserialize(reader, typePath, header, bytes.Length);

        extra.Should().BeOfType<ConveyorData>();
        var conveyor = (ConveyorData)extra!;
        conveyor.Items.Should().ContainSingle();
        var stateful = conveyor.Items.First().Should().BeOfType<StatefulItem>().Subject;
        stateful.FINItemStateFileSystem.Should().Be("ABCD");
        stateful.Position.Should().Be(new Vector4I(1, 2, 3, 4));
    }
}
