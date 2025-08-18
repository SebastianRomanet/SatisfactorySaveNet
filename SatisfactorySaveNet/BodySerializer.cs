using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Exceptions;
using SatisfactorySaveNet.Abstracts.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace SatisfactorySaveNet;

public class BodySerializer : IBodySerializer
{
    public static readonly IBodySerializer Instance = new BodySerializer(StringSerializer.Instance, ObjectHeaderSerializer.Instance, ObjectReferenceSerializer.Instance, ObjectSerializer.Instance);

    private readonly IStringSerializer _stringSerializer;
    private readonly IObjectHeaderSerializer _objectHeaderSerializer;
    private readonly IObjectReferenceSerializer _objectReferenceSerializer;
    private readonly IObjectSerializer _objectSerializer;

    public BodySerializer(IStringSerializer stringSerializer, IObjectHeaderSerializer objectHeaderSerializer, IObjectReferenceSerializer objectReferenceSerializer, IObjectSerializer objectSerializer)
    {
        _stringSerializer = stringSerializer;
        _objectHeaderSerializer = objectHeaderSerializer;
        _objectReferenceSerializer = objectReferenceSerializer;
        _objectSerializer = objectSerializer;
    }

    public BodyBase? Deserialize(BinaryReader reader, Header header)
    {
        Grid? grid = null;
        if (header.SaveVersion >= 41)
        {
            var partitionCount = reader.ReadInt32();
            var unknown1 = _stringSerializer.Deserialize(reader);
            var unknown2 = reader.ReadUInt32();
            var headHex1 = reader.ReadUInt32();
            _ = reader.ReadInt32();
            var unknown4 = _stringSerializer.Deserialize(reader);
            var headHex2 = reader.ReadUInt32();

            var data = new GridData[partitionCount - 1];

            for (var x = 1; x < partitionCount; x++)
            {
                var unknown6 = _stringSerializer.Deserialize(reader);
                var gridHex = reader.ReadUInt32();
                var count = reader.ReadUInt32();
                var nrLevels = reader.ReadInt32();

                var levels = new GridLevel[nrLevels];

                for (var y = 0; y < nrLevels; y++)
                {
                    var unknown9 = _stringSerializer.Deserialize(reader);
                    var unknown10 = reader.ReadUInt32();

                    levels[y] = new GridLevel
                    {
                        Unknown1 = unknown9,
                        Unknown2 = unknown10
                    };
                }

                data[x - 1] = new GridData
                {
                    Unknown1 = unknown6,
                    GridHex = gridHex,
                    Count = count,
                    Levels = levels
                };
            }

            grid = new Grid
            {
                Unknown1 = unknown1,
                Unknown2 = unknown2,
                HeadHex1 = headHex1,
                Unknown4 = unknown4,
                HeadHex2 = headHex2,
                Data = data
            };
        }
        if (header.SaveVersion >= 29)
        {
            var nrLevels = reader.ReadInt32();
            var levels = new List<Level>(nrLevels);

            for (var i = 0; i <= nrLevels; i++)
            {
                var levelName = i == nrLevels ? "Level " + header.MapName : _stringSerializer.Deserialize(reader);
                var binaryLength = header.SaveVersion >= 41 ? reader.ReadInt64() : reader.ReadInt32();
                var position = reader.BaseStream.Position;
                int? saveVersion = null;

                if (header.SaveVersion >= 51)
                {
                    if (i == nrLevels)
                    {
                        saveVersion = header.SaveVersion;
                    }
                    else
                    {
                        reader.BaseStream.Seek(binaryLength, SeekOrigin.Current);
                        var skip = reader.ReadInt64();
                        reader.BaseStream.Seek(skip, SeekOrigin.Current);
                        saveVersion = reader.ReadInt32();
                        reader.BaseStream.Seek(-(skip + binaryLength + sizeof(int) + sizeof(long)), SeekOrigin.Current);
                    }
                }

                var nrObjectHeaders = reader.ReadInt32();
                var objects = new List<ComponentObject>(nrObjectHeaders);

                for (var j = 0; j < nrObjectHeaders; j++)
                {
                    objects.Add(_objectHeaderSerializer.Deserialize(reader, saveVersion));
                }

                List<ObjectReference> collectables;

                if (header.SaveVersion >= 41)
                {
                    if (reader.BaseStream.Position < position + binaryLength - 4)
                    {
                        var nrCollectables = reader.ReadInt32();
                        collectables = new List<ObjectReference>(nrCollectables);
                        for (var j = 0; j < nrCollectables; j++)
                        {
                            collectables.Add(_objectReferenceSerializer.Deserialize(reader));
                        }
                    }
                    else if (reader.BaseStream.Position == position + binaryLength - 4)
                    {
                        reader.ReadInt32();
                        collectables = [];
                    }
                    else
                        collectables = [];
                }
                else
                {
                    var nrCollectables = reader.ReadInt32();
                    collectables = new List<ObjectReference>(nrCollectables);

                    for (var j = 0; j < nrCollectables; j++)
                    {
                        collectables.Add(_objectReferenceSerializer.Deserialize(reader));
                    }
                }

                var binarySizeObjects = header.SaveVersion >= 41 ? reader.ReadInt64() : reader.ReadInt32();
                var positionStart = reader.BaseStream.Position;
                var nrObjects = reader.ReadInt32();

                if (nrObjects != nrObjectHeaders)
                    throw new CorruptedSatisFactorySaveFileException("NrObjects does not match nrObjectHeaders");

                for (var j = 0; j < nrObjects; j++)
                {
                    objects[j] = _objectSerializer.Deserialize(reader, header, objects[j]);
                }

                var expectedPosition = positionStart + binarySizeObjects;
                if (expectedPosition != reader.BaseStream.Position)
                    throw new BadReadException("Expected stream position does not match actual position");

                if (i != nrLevels && header.SaveVersion >= 51)
                    _ = reader.ReadUInt32();

                var nrSecondCollectables = reader.ReadInt32();
                var secondCollectables = new List<ObjectReference>(nrSecondCollectables);

                for (var j = 0; j < nrSecondCollectables; j++)
                {
                    secondCollectables.Add(_objectReferenceSerializer.Deserialize(reader));
                }

#pragma warning disable CS0618 // Type or member is obsolete
                levels.Add(new Level
                {
                    Name = levelName,
                    Objects = objects,
                    Collectables = collectables,
                    SecondCollectables = secondCollectables
                });
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (reader.BaseStream.Position == reader.BaseStream.Length)
            {
                return new BodyV8
                {
                    Levels = levels,
                    Grid = grid
                };
            }

            var nrObjectReferences = reader.ReadInt32();
            var objectReferences = new ObjectReference[nrObjectReferences];

            for (var i = 0; i < nrObjectReferences; i++)
            {
                objectReferences[i] = _objectReferenceSerializer.Deserialize(reader);
            }

#pragma warning disable CS0618 // Type or member is obsolete
            return new BodyV8
            {
                Levels = levels,
                Grid = grid,
                ObjectReferences = objectReferences
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }
        else
        {
            var nrObjectHeaders = reader.ReadInt32();
            var objects = new List<ComponentObject>(nrObjectHeaders);

            for (var j = 0; j < nrObjectHeaders; j++)
            {
                objects.Add(_objectHeaderSerializer.Deserialize(reader, null));
            }

            var nrObjects = reader.ReadInt32();
            
            if (nrObjects != nrObjectHeaders)
                throw new CorruptedSatisFactorySaveFileException("NrObjects does not match nrObjectHeaders");
            
            for (var j = 0; j < nrObjects; j++)
            {
                objects[j] = _objectSerializer.Deserialize(reader, header, objects[j]);
            }
            
            var nrSecondCollectables = reader.ReadInt32();
            var collectables = new List<ObjectReference>(nrSecondCollectables);
            
            for (var j = 0; j < nrSecondCollectables; j++)
            {
                collectables.Add(_objectReferenceSerializer.Deserialize(reader));
            }
#pragma warning disable CS0618 // Type or member is obsolete
            return new BodyPreV8
            {
                Collectables = collectables,
                Objects = objects,
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    public void Serialize(BinaryWriter writer, Header header, BodyBase body)
    {
        switch (body)
        {
            case BodyPreV8 preV8:
                SerializePreV8(writer, preV8);
                break;
            default:
                throw new NotSupportedException("Body serialization for this version is not implemented");
        }
    }

    private void SerializePreV8(BinaryWriter writer, BodyPreV8 body)
    {
        writer.Write(body.Objects.Count);
        foreach (var obj in body.Objects)
        {
            SerializeObjectHeader(writer, obj);
        }

        writer.Write(body.Objects.Count);
        foreach (var obj in body.Objects)
        {
            SerializeObject(writer, obj);
        }

        writer.Write(body.Collectables.Count);
        foreach (var collectable in body.Collectables)
        {
            SerializeObjectReference(writer, collectable);
        }
    }

    private void SerializeObjectHeader(BinaryWriter writer, ComponentObject obj)
    {
        switch (obj)
        {
            case ActorObject actor:
                writer.Write(ActorObject.TypeID);
                _stringSerializer.Serialize(writer, actor.TypePath);
                SerializeObjectReference(writer, actor.ObjectReference);
                if (actor.Flags != null)
                    throw new NotSupportedException("Actor flags serialization not implemented");
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
                break;
            case ComponentObject component:
                writer.Write(ComponentObject.TypeID);
                _stringSerializer.Serialize(writer, component.TypePath);
                SerializeObjectReference(writer, component.ObjectReference);
                if (component.Flags != null)
                    throw new NotSupportedException("Component flags serialization not implemented");
                _stringSerializer.Serialize(writer, component.ParentActorName);
                break;
            default:
                throw new NotSupportedException($"Object header serialization not implemented for type {obj.GetType().Name}");
        }
    }

    private void SerializeObject(BinaryWriter writer, ComponentObject obj)
    {
        switch (obj)
        {
            case ActorObject actor:
                if (actor.Properties.Count != 0)
                    throw new NotSupportedException("Actor property serialization not implemented");
                if (actor.ExtraData != null)
                    throw new NotSupportedException("Actor extra data serialization not implemented");

                using (var ms = new MemoryStream())
                {
                    using var bw = new BinaryWriter(ms);
                    _stringSerializer.Serialize(bw, actor.ParentObjectRoot);
                    _stringSerializer.Serialize(bw, actor.ParentObjectName);
                    bw.Write(actor.Components.Count);
                    foreach (var componentRef in actor.Components)
                        SerializeObjectReference(bw, componentRef);
                    bw.Flush();
                    writer.Write((int)ms.Length);
                    writer.Write(ms.ToArray());
                }
                break;
            case ComponentObject component:
                if (component.Properties.Count != 0)
                    throw new NotSupportedException("Component property serialization not implemented");
                if (component.ExtraData != null)
                    throw new NotSupportedException("Component extra data serialization not implemented");

                using (var ms = new MemoryStream())
                {
                    using var bw = new BinaryWriter(ms);
                    _stringSerializer.Serialize(bw, "None");
                    bw.Flush();
                    writer.Write((int)ms.Length);
                    writer.Write(ms.ToArray());
                }
                break;
            default:
                throw new NotSupportedException($"Object serialization not implemented for type {obj.GetType().Name}");
        }
    }

    private void SerializeObjectReference(BinaryWriter writer, ObjectReference reference)
    {
        _stringSerializer.Serialize(writer, reference.LevelName);
        _stringSerializer.Serialize(writer, reference.PathName);
    }
}