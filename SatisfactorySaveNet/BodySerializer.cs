using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Exceptions;
using SatisfactorySaveNet.Abstracts.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                SerializePreV8(writer, header, preV8);
                break;
            case BodyV8 v8:
                SerializeV8(writer, header, v8);
                break;
            default:
                throw new NotSupportedException("Body serialization for this version is not implemented");
        }
    }

    private void SerializePreV8(BinaryWriter writer, Header header, BodyPreV8 body)
    {
        writer.Write(body.Objects.Count);
        foreach (var obj in body.Objects)
        {
            _objectHeaderSerializer.Serialize(writer, obj, null);
        }

        writer.Write(body.Objects.Count);
        foreach (var obj in body.Objects)
        {
            _objectSerializer.Serialize(writer, header, obj);
        }

        writer.Write(body.Collectables.Count);
        foreach (var collectable in body.Collectables)
        {
            _objectReferenceSerializer.Serialize(writer, collectable);
        }
    }

    private void SerializeV8(BinaryWriter writer, Header header, BodyV8 body)
    {
        if (header.SaveVersion < 41)
            throw new NotSupportedException("BodyV8 serialization for save versions below 41 is not implemented");

        if (header.SaveVersion >= 51)
        {
            // Save versions 51+ still include the grid section but introduce
            // additional data for streaming levels.  We currently only support
            // a single persistent level and omit streaming level support.

            if (body.Grid is not null)
                throw new NotSupportedException("Grid serialization not implemented");

            if (body.Levels.Count != 1)
                throw new NotSupportedException("BodyV8 serialization only supports a single persistent level");

            var level = body.Levels.First();

            // minimal grid
            writer.Write(1); // partition count
            _stringSerializer.Serialize(writer, string.Empty);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0);
            _stringSerializer.Serialize(writer, string.Empty);
            writer.Write(0u);

            // no non-persistent levels
            writer.Write(0);

            using var levelStream = new MemoryStream();
            using (var levelWriter = new BinaryWriter(levelStream, System.Text.Encoding.UTF8, true))
            {
                // nrObjectHeaders
                levelWriter.Write(level.Objects.Count);
                foreach (var obj in level.Objects)
                {
                    _objectHeaderSerializer.Serialize(levelWriter, obj, header.SaveVersion);
                }

                // nrCollectables
                levelWriter.Write(level.Collectables.Count);
                foreach (var collectable in level.Collectables)
                {
                    _objectReferenceSerializer.Serialize(levelWriter, collectable);
                }

                using var objectStream = new MemoryStream();
                using (var objectWriter = new BinaryWriter(objectStream, System.Text.Encoding.UTF8, true))
                {
                    objectWriter.Write(level.Objects.Count);
                    foreach (var obj in level.Objects)
                    {
                        _objectSerializer.Serialize(objectWriter, header, obj);
                    }
                }

                levelWriter.Write((long)objectStream.Length);
                levelWriter.Write(objectStream.ToArray());

                // SaveVersion 51+ adds an unknown 32-bit field between objects and
                // second collectables for streaming levels.  Streaming levels are
                // not supported yet, so nothing is written here.

                var secondCollectables = level.SecondCollectables ?? Enumerable.Empty<ObjectReference>();
                levelWriter.Write(secondCollectables.Count());
                foreach (var collectable in secondCollectables)
                {
                    _objectReferenceSerializer.Serialize(levelWriter, collectable);
                }
            }

            writer.Write(levelStream.Length);
            writer.Write(levelStream.ToArray());

            if (body.ObjectReferences is { Count: > 0 })
            {
                writer.Write(body.ObjectReferences.Count);
                foreach (var objRef in body.ObjectReferences)
                {
                    _objectReferenceSerializer.Serialize(writer, objRef);
                }
            }

            return;
        }

        // Save versions 41-50.  These include a grid section and allow a single
        // persistent level with optional objects, collectables and references.

        // Only support a single persistent level with no grid data
        if (body.Grid is not null)
            throw new NotSupportedException("Grid serialization not implemented");

        if (body.Levels.Count != 1)
            throw new NotSupportedException("BodyV8 serialization only supports a single persistent level");

        var level41 = body.Levels.First();

        // minimal grid
        writer.Write(1); // partition count
        _stringSerializer.Serialize(writer, string.Empty);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0);
        _stringSerializer.Serialize(writer, string.Empty);
        writer.Write(0u);

        // no non-persistent levels
        writer.Write(0);

        using var levelStream41 = new MemoryStream();
        using (var levelWriter41 = new BinaryWriter(levelStream41, System.Text.Encoding.UTF8, true))
        {
            // nrObjectHeaders
            levelWriter41.Write(level41.Objects.Count);
            foreach (var obj in level41.Objects)
            {
                _objectHeaderSerializer.Serialize(levelWriter41, obj, header.SaveVersion);
            }

            // nrCollectables
            levelWriter41.Write(level41.Collectables.Count);
            foreach (var collectable in level41.Collectables)
            {
                _objectReferenceSerializer.Serialize(levelWriter41, collectable);
            }

            using var objectStream41 = new MemoryStream();
            using (var objectWriter41 = new BinaryWriter(objectStream41, System.Text.Encoding.UTF8, true))
            {
                objectWriter41.Write(level41.Objects.Count);
                foreach (var obj in level41.Objects)
                {
                    _objectSerializer.Serialize(objectWriter41, header, obj);
                }
            }

            levelWriter41.Write((long)objectStream41.Length);
            levelWriter41.Write(objectStream41.ToArray());

            var secondCollectables41 = level41.SecondCollectables ?? Enumerable.Empty<ObjectReference>();
            levelWriter41.Write(secondCollectables41.Count());
            foreach (var collectable in secondCollectables41)
            {
                _objectReferenceSerializer.Serialize(levelWriter41, collectable);
            }
        }

        writer.Write(levelStream41.Length);
        writer.Write(levelStream41.ToArray());

        if (body.ObjectReferences is { Count: > 0 })
        {
            writer.Write(body.ObjectReferences.Count);
            foreach (var objRef in body.ObjectReferences)
            {
                _objectReferenceSerializer.Serialize(writer, objRef);
            }
        }
    }
}
