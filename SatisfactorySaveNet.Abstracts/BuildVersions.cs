using System.Collections.Generic;

namespace SatisfactorySaveNet.Abstracts;

/// <summary>
/// Constants for known Satisfactory build versions.
/// </summary>
public static class BuildVersions
{
    /// <summary>
    /// First patch of Update 4 on the experimental branch.
    /// </summary>
    public const int Patch0400 = 146871;

    /// <summary>
    /// Update 6 hotfix 0.6.1.3.
    /// </summary>
    public const int Patch0613 = 202470;

    /// <summary>
    /// First experimental release of Update 7.
    /// </summary>
    public const int Patch0700 = 208250;

    /// <summary>
    /// Release build for Version 1.0.
    /// </summary>
    public const int Patch1000 = 424353;

    private static readonly IReadOnlyDictionary<int, string> _names = new Dictionary<int, string>
    {
        [Patch0400] = "Update 4 (0.4.0.0)",
        [Patch0613] = "Patch 0.6.1.3",
        [Patch0700] = "Patch 0.7.0.0",
        [Patch1000] = "Patch 1.0.0.0"
    };

    /// <summary>
    /// Determines whether the specified build version is known.
    /// </summary>
    public static bool IsKnown(int buildVersion) => _names.ContainsKey(buildVersion);

    /// <summary>
    /// Gets a friendly name for a build version, if available.
    /// </summary>
    public static string? GetName(int buildVersion) =>
        _names.TryGetValue(buildVersion, out var name) ? name : null;
}
