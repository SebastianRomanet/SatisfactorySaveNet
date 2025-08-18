using SatisfactorySaveNet.Abstracts;
using System.IO;
using System.Linq;

namespace SatisfactorySaveNet;

public class HexSerializer : IHexSerializer
{
    public static readonly HexSerializer Instance = new();

    public string Deserialize(BinaryReader reader, int length)
    {
        var hexChars = new char[length];

        for (var i = 0; i < length; i++)
        {
            var hexChar = (char) reader.ReadByte();
            hexChars[i] = hexChar;
        }

        return new string([.. hexChars]);
    }

    public void Serialize(BinaryWriter writer, string hex)
    {
        foreach (var c in hex)
            writer.Write((byte)c);
    }
}
