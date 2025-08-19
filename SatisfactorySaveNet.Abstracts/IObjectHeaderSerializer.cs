using SatisfactorySaveNet.Abstracts.Model;
using System.IO;

namespace SatisfactorySaveNet.Abstracts;

public interface IObjectHeaderSerializer
{
    public ComponentObject Deserialize(BinaryReader reader, int? saveVersion);
    public void Serialize(BinaryWriter writer, ComponentObject obj, int? saveVersion);
}
