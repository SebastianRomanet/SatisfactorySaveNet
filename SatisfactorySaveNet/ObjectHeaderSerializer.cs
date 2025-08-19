using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Exceptions;
using SatisfactorySaveNet.Abstracts.Model;
using System.IO;

namespace SatisfactorySaveNet;

public class ObjectHeaderSerializer : IObjectHeaderSerializer
{
    public static readonly IObjectHeaderSerializer Instance = new ObjectHeaderSerializer(StringSerializer.Instance, VectorSerializer.Instance, ObjectReferenceSerializer.Instance);

    private readonly IStringSerializer _stringSerializer;
    private readonly IVectorSerializer _vectorSerializer;
    private readonly IObjectReferenceSerializer _objectReferenceSerializer;

    public ObjectHeaderSerializer(IStringSerializer stringSerializer, IVectorSerializer vectorSerializer, IObjectReferenceSerializer objectReferenceSerializer)
    {
        _stringSerializer = stringSerializer;
        _vectorSerializer = vectorSerializer;
        _objectReferenceSerializer = objectReferenceSerializer;
    }

    public ComponentObject Deserialize(BinaryReader reader, int? saveVersion)
    {
        var type = reader.ReadInt32();
        return type switch
        {
            ComponentObject.TypeID => DeserializeComponentHeader(reader, saveVersion),
            ActorObject.TypeID => DeserializeActorHeader(reader, saveVersion),
            _ => throw new CorruptedSatisFactorySaveFileException("Encountered unknown object type")
        };
    }

    public void Serialize(BinaryWriter writer, ComponentObject obj, int? saveVersion)
    {
        writer.Write(obj.Type);
        switch (obj)
        {
            case ActorObject actor:
                SerializeActorHeader(writer, actor, saveVersion);
                break;
            case ComponentObject component:
                SerializeComponentHeader(writer, component, saveVersion);
                break;
            default:
                throw new CorruptedSatisFactorySaveFileException("Encountered unknown object type");
        }
    }

    private ActorObject DeserializeActorHeader(BinaryReader reader, int? saveVersion)
    {
        var typePath = _stringSerializer.Deserialize(reader);
        var objectReference = _objectReferenceSerializer.Deserialize(reader);
        
        uint? flags = null;
        if (saveVersion >= 51)
            flags = reader.ReadUInt32();

        var needTransform = reader.ReadInt32();
        var rotation = _vectorSerializer.DeserializeVec4(reader);
        var position = _vectorSerializer.DeserializeVec3(reader);
        var scale = _vectorSerializer.DeserializeVec3(reader);
        var wasPlacedInLevel = reader.ReadInt32();

        return new ActorObject
        {
            TypePath = typePath,
            ObjectReference = objectReference,
            NeedTransform = needTransform,
            Rotation = rotation,
            Position = position,
            Scale = scale,
            PlacedInLevel = wasPlacedInLevel,
            Flags = flags
        };
    }

    private void SerializeActorHeader(BinaryWriter writer, ActorObject actor, int? saveVersion)
    {
        _stringSerializer.Serialize(writer, actor.TypePath);
        _objectReferenceSerializer.Serialize(writer, actor.ObjectReference);
        if (saveVersion >= 51)
            writer.Write(actor.Flags ?? 0);
        writer.Write(actor.NeedTransform);
        writer.Write(actor.Rotation.X);
        writer.Write(actor.Rotation.Y);
        writer.Write(actor.Rotation.Z);
        writer.Write(actor.Rotation.W);
        writer.Write(actor.Position.X);
        writer.Write(actor.Position.Y);
        writer.Write(actor.Position.Z);
        writer.Write(actor.Scale.X);
        writer.Write(actor.Scale.Y);
        writer.Write(actor.Scale.Z);
        writer.Write(actor.PlacedInLevel);
    }

    private ComponentObject DeserializeComponentHeader(BinaryReader reader, int? saveVersion)
    {
        var typePath = _stringSerializer.Deserialize(reader);
        var objectReference = _objectReferenceSerializer.Deserialize(reader);

        uint? flags = null;
        if (saveVersion >= 51)
            flags = reader.ReadUInt32();

        var parentActorName = _stringSerializer.Deserialize(reader);

        return new ComponentObject
        {
            TypePath = typePath,
            ObjectReference = objectReference,
            ParentActorName = parentActorName,
            Flags = flags
        };
    }

    private void SerializeComponentHeader(BinaryWriter writer, ComponentObject component, int? saveVersion)
    {
        _stringSerializer.Serialize(writer, component.TypePath);
        _objectReferenceSerializer.Serialize(writer, component.ObjectReference);
        if (saveVersion >= 51)
            writer.Write(component.Flags ?? 0);
        _stringSerializer.Serialize(writer, component.ParentActorName);
    }
}
