using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SatisfactorySaveNet;
using SatisfactorySaveNet.Abstracts.Model;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LatestSaveRoundtripTests
{
    public static IEnumerable<TestCaseData> SaveFiles()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../"));
        var exampleDir = Path.Combine(repoRoot, "SatisfactorySaveNet", "Example Files");
        foreach (var file in Directory.EnumerateFiles(exampleDir, "*.sav"))
        {
            yield return new TestCaseData(file).SetName(Path.GetFileName(file));
        }
    }

    [TestCaseSource(nameof(SaveFiles))]
    public void Save_Roundtrips_Correctly(string path)
    {
        var originalSave = SaveFileSerializer.Instance.Deserialize(path);

        var roundtripData = SaveFileSerializer.Instance.Serialize(originalSave);
        var roundtripped = SaveFileSerializer.Instance.Deserialize(roundtripData);

        roundtripped.Header.Should().BeEquivalentTo(originalSave.Header);
        roundtripped.Header.SaveName.Should().Be(originalSave.Header.SaveName);
        roundtripped.Header.IsCreativeModeEnabled.Should().Be(originalSave.Header.IsCreativeModeEnabled);
        roundtripped.Body.Should().BeEquivalentTo(originalSave.Body);
    }
}

