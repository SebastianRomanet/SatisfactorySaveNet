using SatisfactorySaveNet.Abstracts;
using SatisfactorySaveNet.Abstracts.Model;
using System;
using System.IO;

namespace SatisfactorySaveNet;

public class HeaderSerializer : IHeaderSerializer
{
    public static readonly IHeaderSerializer Instance = new HeaderSerializer(StringSerializer.Instance, HexSerializer.Instance);

    private readonly IStringSerializer _stringSerializer;
    private readonly IHexSerializer _hexSerializer;

    public HeaderSerializer(IStringSerializer stringSerializer, IHexSerializer hexSerializer)
    {
        _stringSerializer = stringSerializer;
        _hexSerializer = hexSerializer;
    }

    public Header Deserialize(BinaryReader reader)
    {
        var headerVersion = reader.ReadInt32();
        var saveVersion = reader.ReadInt32();
        var buildVersion = reader.ReadInt32();

        if (headerVersion > (int)SaveHeaderVersion.LatestVersion)
            throw new NotSupportedException($"Unsupported header version {headerVersion}. Supported up to {(int)SaveHeaderVersion.LatestVersion}.");

        if (saveVersion < (int)FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents || saveVersion > (int)FSaveCustomVersion.LatestVersion)
            throw new NotSupportedException($"Unsupported save version {saveVersion}. Supported range {(int)FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents}-{(int)FSaveCustomVersion.LatestVersion}.");

        if (!BuildVersions.IsKnown(buildVersion))
            throw new NotSupportedException($"Unsupported build version {buildVersion}.");

        string? saveName = null;

        if (headerVersion >= 14)
            saveName = _stringSerializer.Deserialize(reader);

        var header = new Header
        {
            HeaderVersion = headerVersion,
            SaveVersion = saveVersion,
            BuildVersion = buildVersion,
            SaveName = saveName,

            MapName = _stringSerializer.Deserialize(reader),
            MapOptions = _stringSerializer.Deserialize(reader),
            SessionName = _stringSerializer.Deserialize(reader),

            PlayedSeconds = reader.ReadInt32(),
            SaveDateTimeUtc = new DateTime(reader.ReadInt64(), DateTimeKind.Utc),
        };

        //ToDo: Set flag to inform about possible loss of information due to deprecated reader

        //if (header.HeaderVersion > SaveHeaderVersion.LatestVersion)
        //if (header.SaveVersion < FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents || header.SaveVersion > FSaveCustomVersion.LatestVersion)

        if (header.HeaderVersion >= 5)
            header.SessionVisibility = reader.ReadByte();

        if (header.HeaderVersion >= 7)
            header.EditorObjectVersion = reader.ReadInt32();

        if (header.HeaderVersion >= 8)
        {
            header.ModMetadata = _stringSerializer.Deserialize(reader);
            header.IsModdedSave = reader.ReadInt32();
        }

        if (header.HeaderVersion >= 10)
            header.SaveIdentifier = _stringSerializer.Deserialize(reader);

        if (header.HeaderVersion >= 13)
        {
            header.IsPartitionedWorld = reader.ReadInt32();
            header.SaveDataHash = _hexSerializer.Deserialize(reader, 20);
            header.IsCreativeModeEnabled = reader.ReadInt32();
        }

            return header;
    }

    public void Serialize(BinaryWriter writer, Header header)
    {
        writer.Write(header.HeaderVersion);
        writer.Write(header.SaveVersion);

        if (!BuildVersions.IsKnown(header.BuildVersion))
            throw new NotSupportedException($"Unsupported build version {header.BuildVersion}.");

        writer.Write(header.BuildVersion);

        if (header.HeaderVersion >= 14)
            _stringSerializer.Serialize(writer, header.SaveName ?? string.Empty);

        _stringSerializer.Serialize(writer, header.MapName);
        _stringSerializer.Serialize(writer, header.MapOptions);
        _stringSerializer.Serialize(writer, header.SessionName);

        writer.Write(header.PlayedSeconds);
        writer.Write(header.SaveDateTimeUtc.Ticks);

        if (header.HeaderVersion >= 5)
            writer.Write(header.SessionVisibility ?? 0);

        if (header.HeaderVersion >= 7)
            writer.Write(header.EditorObjectVersion ?? 0);

        if (header.HeaderVersion >= 8)
        {
            _stringSerializer.Serialize(writer, header.ModMetadata ?? string.Empty);
            writer.Write(header.IsModdedSave ?? 0);
        }

        if (header.HeaderVersion >= 10)
            _stringSerializer.Serialize(writer, header.SaveIdentifier ?? string.Empty);

        if (header.HeaderVersion >= 13)
        {
            writer.Write(header.IsPartitionedWorld ?? 0);
            _hexSerializer.Serialize(writer, header.SaveDataHash ?? string.Empty);
            writer.Write(header.IsCreativeModeEnabled ?? 0);
        }
    }
}
