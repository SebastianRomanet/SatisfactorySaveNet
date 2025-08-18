using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;
using SatisfactorySaveNet.Abstracts.Model.Properties;
using System.IO;
using System.Linq;

namespace SatisfactorySaveNet;

internal static class InventoryItemHelper
{
    internal static (string? FINItemStateFileSystem, Property[]? Properties) ReadStatefulItem(
        BinaryReader reader,
        Header header,
        IPropertySerializer propertySerializer,
        IHexSerializer hexSerializer,
        string? itemStatePath,
        long? expectedPosition = null)
    {
        if (string.Equals(itemStatePath, "/Script/FicsItNetworksComputer.FINItemStateFileSystem"))
        {
            var length = reader.ReadInt32();
            var finItemStateFileSystem = hexSerializer.Deserialize(reader, length);
            return (finItemStateFileSystem, null);
        }

        var properties = propertySerializer.DeserializeProperties(reader, header, expectedPosition: expectedPosition).ToArray();
        return (null, properties);
    }
}
