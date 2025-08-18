using System.IO;

namespace SatisfactorySaveNet.Abstracts;

public interface IStringSerializer
{
    public string Deserialize(BinaryReader reader);
    public void Serialize(BinaryWriter writer, string value);
}