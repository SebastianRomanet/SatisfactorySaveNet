using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Exceptions;
using SatisfactorySaveNet.Abstracts.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SatisfactorySaveNet;

public class ObjectSerializer : IObjectSerializer
{
    public static readonly IObjectSerializer Instance = new ObjectSerializer(NullLoggerFactory.Instance, StringSerializer.Instance, ObjectReferenceSerializer.Instance, PropertySerializer.Instance, ExtraDataSerializer.Instance, HexSerializer.Instance);

    private readonly IStringSerializer _stringSerializer;
    private readonly IObjectReferenceSerializer _objectReferenceSerializer;
    private readonly IPropertySerializer _propertySerializer;
    private readonly IExtraDataSerializer _extraDataSerializer;
    private readonly IHexSerializer _hexSerializer;
    private readonly ILogger<ObjectSerializer> _logger;

    public ObjectSerializer(ILoggerFactory loggerFactory, IStringSerializer stringSerializer, IObjectReferenceSerializer objectReferenceSerializer, IPropertySerializer propertySerializer, IExtraDataSerializer extraDataSerializer, IHexSerializer hexSerializer)
    {
        _stringSerializer = stringSerializer;
        _objectReferenceSerializer = objectReferenceSerializer;
        _propertySerializer = propertySerializer;
        _extraDataSerializer = extraDataSerializer;
        _hexSerializer = hexSerializer;
        _logger = loggerFactory.CreateLogger<ObjectSerializer>();
    }

    public ComponentObject Deserialize(BinaryReader reader, Header header, ComponentObject componentObject)
    {
        return componentObject switch
        {
            ActorObject actorObject => DeserializeActor(reader, header, actorObject),
            ComponentObject => DeserializeComponent(reader, header, componentObject),
            _ => throw new CorruptedSatisFactorySaveFileException("Encountered unknown object type")
        };
    }

    public void Serialize(BinaryWriter writer, Header header, ComponentObject obj)
    {
        switch (obj)
        {
            case ActorObject actor:
                SerializeActor(writer, header, actor);
                break;
            case ComponentObject component:
                SerializeComponent(writer, header, component);
                break;
            default:
                throw new CorruptedSatisFactorySaveFileException("Encountered unknown object type");
        }
    }

    private ActorObject DeserializeActor(BinaryReader reader, Header header, ActorObject actorObject)
    {
        if (header.SaveVersion >= 41)
        {
            var version = reader.ReadInt32();
            if (version != header.SaveVersion)
                actorObject.EntitySaveVersion = version;
            _ = reader.ReadInt32();
        }
        var binarySize = reader.ReadInt32();
        var positionStart = reader.BaseStream.Position;

        var parentObjectRoot = _stringSerializer.Deserialize(reader);

        var parentObjectName = _stringSerializer.Deserialize(reader);

        var componentsCount = reader.ReadInt32();
        var components = new List<ObjectReference>(componentsCount);

        for (var i = 0; i < componentsCount; i++)
        {
            var objectRef = _objectReferenceSerializer.Deserialize(reader);
            components.Add(objectRef);
        }

        actorObject.ParentObjectRoot = parentObjectRoot;
        actorObject.ParentObjectName = parentObjectName;

        var expectedPosition = positionStart + binarySize;
        actorObject.Components = components;

        if (expectedPosition == reader.BaseStream.Position)
            return actorObject;

        var properties = _propertySerializer.DeserializeProperties(reader, header, expectedPosition: expectedPosition).ToArray();

        actorObject.Properties = properties;
        actorObject.ExtraData = _extraDataSerializer.Deserialize(reader, actorObject.TypePath, header, expectedPosition);

        var missingBytes = expectedPosition - reader.BaseStream.Position;

        if (missingBytes > 0)
        {
            var hex = _hexSerializer.Deserialize(reader, missingBytes.ToInt());
            if (hex.Any(c => c != '\0'))
                _logger.LogWarning("Missing bytes: {MissingBytes}", hex);
        }
        else if (missingBytes < 0)
            reader.BaseStream.Seek(missingBytes, SeekOrigin.Current);

        return actorObject;
    }

    private void SerializeActor(BinaryWriter writer, Header header, ActorObject actor)
    {
        if (header.SaveVersion >= 41)
        {
            writer.Write(actor.EntitySaveVersion ?? header.SaveVersion);
            writer.Write(0);
        }

        using var data = new MemoryStream();
        using (var bw = new BinaryWriter(data, System.Text.Encoding.UTF8, true))
        {
            _stringSerializer.Serialize(bw, actor.ParentObjectRoot);
            _stringSerializer.Serialize(bw, actor.ParentObjectName);
            bw.Write(actor.Components.Count);
            foreach (var comp in actor.Components)
                _objectReferenceSerializer.Serialize(bw, comp);
            // no properties or extra data supported
        }

        writer.Write((int)data.Length);
        writer.Write(data.ToArray());
    }

    private ComponentObject DeserializeComponent(BinaryReader reader, Header header, ComponentObject componentObject)
    {
        if (header.SaveVersion >= 41)
        {
            var version = reader.ReadInt32();
            if (version != header.SaveVersion)
                componentObject.EntitySaveVersion = version;
            _ = reader.ReadInt32();
        }
        var binarySize = reader.ReadInt32();
        var positionStart = reader.BaseStream.Position;

        var properties = _propertySerializer.DeserializeProperties(reader, header).ToArray();
        componentObject.Properties = properties;

        var expectedPosition = positionStart + binarySize;
        var missingBytes = expectedPosition - reader.BaseStream.Position;

        if (missingBytes > 0)
        {
            var hex = _hexSerializer.Deserialize(reader, missingBytes.ToInt());
            if (hex.Any(c => c != '\0'))
                _logger.LogCritical("BAD READ {MissingBytes}", missingBytes);
        }
        else if (missingBytes < 0)
            reader.BaseStream.Seek(missingBytes, SeekOrigin.Current);

        return componentObject;
    }

    private void SerializeComponent(BinaryWriter writer, Header header, ComponentObject component)
    {
        if (header.SaveVersion >= 41)
        {
            writer.Write(component.EntitySaveVersion ?? header.SaveVersion);
            writer.Write(0);
        }

        using var data = new MemoryStream();
        using (var bw = new BinaryWriter(data, System.Text.Encoding.UTF8, true))
        {
            // write property terminator
            _stringSerializer.Serialize(bw, "None");
        }

        writer.Write((int)data.Length);
        writer.Write(data.ToArray());
    }
}
