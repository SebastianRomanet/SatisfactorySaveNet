using FluentAssertions;
using NUnit.Framework;
using SatisfactorySaveNet.Abstracts;

namespace SatisfactorySaveNet.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class BuildVersionsTests
{
    [Test]
    public void IsKnown_ReturnsTrue_ForPatch1000()
    {
        BuildVersions.IsKnown(BuildVersions.Patch1000).Should().BeTrue();
        BuildVersions.GetName(BuildVersions.Patch1000).Should().Be("Patch 1.0.0.0");
    }
}

