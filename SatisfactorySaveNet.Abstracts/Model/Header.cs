using System;

namespace SatisfactorySaveNet.Abstracts.Model;

public class Header
{
    /// <summary>
    /// Version of the header structure.
    /// See <see cref="SaveHeaderVersion"/> for known values.
    /// </summary>
    public int HeaderVersion { get; set; }

    /// <summary>
    /// Version of the save serialization format.
    /// See <see cref="FSaveCustomVersion"/> for known values.
    /// </summary>
    public int SaveVersion { get; set; }

    /// <summary>
    /// Build number of the game.
    /// Known builds are listed in <see cref="BuildVersions"/>.
    /// </summary>
    public int BuildVersion { get; set; }

    /// <summary>
    /// Friendly name of <see cref="BuildVersion"/> if known.
    /// </summary>
    public string? BuildVersionName => BuildVersions.GetName(BuildVersion);

    /// <summary>
    /// "Persistent_Level"
    /// </summary>
    public string MapName { get; set; } = string.Empty;

    /// <summary>
    /// An URL style list of arguments of the session.
    /// Contains the startLocation, sessionName and visibility
    /// </summary>
    public string MapOptions { get; set; } = string.Empty;

    /// <summary>
    /// Name of the saved game as entered when creating a new game
    /// </summary>
    public string SessionName { get; set; } = string.Empty;

    /// <summary>
    /// Amount of seconds spent in this save
    /// </summary>
    public int PlayedSeconds { get; set; }

    /// <summary>
    /// Unix utc timestamp of when the save was saved
    /// </summary>
    public DateTime SaveDateTimeUtc { get; set; }

    /// <summary>
    /// This is "private" visibility, 1 would be "friends only" 
    /// </summary>
    public byte? SessionVisibility { get; set; }

    /// <summary>
    /// Depends on the <see href="https://docs.unrealengine.com/4.26/en-US/ProgrammingAndScripting/ProgrammingWithCPP/UnrealArchitecture/VersioningAssetsAndPackages/">unreal engine</see> version used 
    /// </summary>
    public int? EditorObjectVersion { get; set; }

    /// <summary>
    /// Empty if no mods where used 
    /// </summary>
    public string ModMetadata { get; set; } = string.Empty;

    /// <summary>
    /// False if no mods where used
    /// IsModdedSave != 0 <=> True
    /// </summary>
    public int? IsModdedSave { get; set; }

    /// <summary>
    /// A unique identifier (<see href="https://en.wikipedia.org/wiki/Universally_unique_identifier">GUID</see>) for this save, for analytics purposes 
    /// </summary>
    public string? SaveIdentifier { get; set; }

    /// <summary>
    /// Unknown yet
    /// IsPartitionedWorld != 0 <=> True
    /// </summary>
    public int? IsPartitionedWorld { get; set; }

    /// <summary>
    /// Propably some hash for the savegame
    /// </summary>
    public string? SaveDataHash { get; set; }

    /// <summary>
    /// Is creative enabled
    /// IsCreativeModeEnabled != 0 <=> True
    /// </summary>
    public int? IsCreativeModeEnabled { get; set; }

    /// <summary>
    /// Name of the save
    /// </summary>
    public string? SaveName { get; set; }
}