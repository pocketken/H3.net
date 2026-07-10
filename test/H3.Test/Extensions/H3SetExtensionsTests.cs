using System;
using System.Collections.Generic;
using NUnit.Framework;

using H3.Algorithms;
using H3.Extensions;
using System.Linq;

namespace H3.Test.Extensions; 

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class H3SetExtensionsTests {

    // select h3_compact(array(select h3_k_ring('8e48e1d7038d527'::h3index, 2)));
    private static readonly H3Index[] TestCompactArray = {
        0x8e48e1d7038dc9f,
        0x8e48e1d7038dcd7,
        0x8e48e1d7038dc8f,
        0x8e48e1d7038dc87,
        0x8e48e1d7038dc97,
        0x8e48e1d7038c26f,
        0x8e48e1d7038c24f,
        0x8e48e1d7038d577,
        0x8e48e1d7038dcdf,
        0x8e48e1d7038dcc7,
        0x8e48e1d7038dcf7,
        0x8e48e1d7038dcaf,
        0x8d48e1d7038d53f
    };

    // select h3_uncompact(array(select h3_compact(array(select h3_k_ring('8e48e1d7038d527'::h3index, 2)))), 14);
    private static readonly H3Index[] TestUncompactArray = {
        0x8e48e1d7038dc9f,
        0x8e48e1d7038dcd7,
        0x8e48e1d7038dc8f,
        0x8e48e1d7038dc87,
        0x8e48e1d7038dc97,
        0x8e48e1d7038c26f,
        0x8e48e1d7038c24f,
        0x8e48e1d7038d577,
        0x8e48e1d7038dcdf,
        0x8e48e1d7038dcc7,
        0x8e48e1d7038dcf7,
        0x8e48e1d7038dcaf,
        0x8e48e1d7038d507,
        0x8e48e1d7038d50f,
        0x8e48e1d7038d517,
        0x8e48e1d7038d51f,
        0x8e48e1d7038d527,
        0x8e48e1d7038d52f,
        0x8e48e1d7038d537
    };

    private static readonly H3Index Sunnyvale = 0x89283470c27ffff;

    private static readonly H3Index[] Uncompactable = {
        0x89283470803ffff,
        0x8928347081bffff,
        0x8928347080bffff
    };

    private static readonly H3Index[] UncompactableWithZero = {
        0x89283470803ffff,
        0x8928347081bffff,
        0,
        0x8928347080bffff
    };

    private static readonly IEnumerable<H3Index> UncompactSomeHexagons = Enumerable.Range(0, 3)
        .Select(i => H3Index.Create(5, i, 0));

    [Test]
    public void Test_Compact_CanCompactMixedResolutions() {
        // Arrange
        H3Index[] indicies = { TestHelpers.SfIndex, (H3Index)TestHelpers.TestIndexValue };

        // Act
        var actual = indicies.CompactCells().ToArray();

        // Assert
        TestHelpers.AssertAll(indicies, actual);
    }

    [Test]
    public void Test_Compact_MatchesPg() {
        // Act
        var result = TestHelpers.TestIndexKRingsTo2.Select(e => (H3Index)e.Item1).CompactCells().ToArray();

        // Assert
        TestHelpers.AssertAll(TestCompactArray, result);
    }

    [Test]
    public void Test_Compact_RemovesDuplicates() {
        // Arrange
        var input = TestHelpers.TestIndexKRingsTo2.Select(e => (H3Index)e.Item1).ToList();
        input.AddRange(TestHelpers.TestIndexKRingsTo2.Take(5).Select(e => (H3Index)e.Item1));

        // Act
        var result = input.CompactCells().ToArray();

        // Assert
        TestHelpers.AssertAll(TestCompactArray, result);
    }

    [Test]
    public void Test_Uncomapct_MatchesPg() {
        // Act
        var result = TestCompactArray.UncompactCells(14).ToArray();

        // Assert
        TestHelpers.AssertAll(TestUncompactArray, result);
    }

    [Test]
    public void Test_Upstream_Compact_Sunnyvale() {
        // Arrange
        var sunnyvaleExpanded = Sunnyvale.GridDiskDistances(9).Select(c => c.Index);

        // Act
        var actual = sunnyvaleExpanded.CompactCells().ToList();

        // Assert
        Assert.That(actual.Count, Is.EqualTo(73), "should reduce to 73 indexes");
    }

    [Test]
    public void Test_Upstream_CompactUncompact_Roundtrip() {
        // Arrange
        var sunnyvaleExpanded = Sunnyvale
            .GridDiskDistances(9)
            .Select(c => c.Index)
            .ToList();
        var expectedCount = sunnyvaleExpanded.Count;

        // Act
        var actual = sunnyvaleExpanded
            .CompactCells()
            .UncompactCells(9)
            .ToList();

        // Assert
        Assert.That(actual.Count, Is.EqualTo(expectedCount), $"should return {expectedCount}");
    }

    [Test]
    public void Test_Upstream_919_Compact_AllRes1Cells() {
        // Arrange
        var allRes1 = H3Index.GetRes0Cells()
            .SelectMany(cell => cell.GetChildrenForResolution(1))
            .ToList();

        // Act
        var actual = allRes1.CompactCells();

        // Assert
        Assert.That(allRes1.Count, Is.EqualTo(842), "should start with 842 res 1 cells");
        Assert.That(actual.Count, Is.EqualTo(122), "should compact to the 122 res 0 cells");
        Assert.That(actual.All(cell => cell.Resolution == 0), Is.True, "all results should be res 0");
    }

    [Test]
    public void Test_Upstream_679_Compact_AllChildrenOfRes0Cell() {
        // Arrange
        var parent = H3Index.Create(0, 0, 0);
        var children = parent.GetChildrenForResolution(1).ToList();

        // Act
        var actual = children.CompactCells();

        // Assert
        Assert.That(actual, Is.EqualTo(new List<H3Index> { parent }), "should compact to the res 0 parent");
    }

    [Test]
    public void Test_GH61_CanonicalizeCells_SortsAndDeduplicates() {
        // Arrange
        var cells = TestHelpers.SfIndex.GridDiskDistances(2)
            .Select(cell => cell.Index)
            .Concat(new[] { TestHelpers.SfIndex, H3Index.Invalid })
            .Reverse()
            .ToList();
        var expected = cells.Where(cell => cell != H3Index.Invalid).Distinct().OrderBy(cell => (ulong)cell).ToList();

        // Act
        var canonical = cells.CanonicalizeCells();

        // Assert
        Assert.That(canonical, Is.EqualTo(expected), "should be sorted ascending and unique without H3_NULL");
    }

    [Test]
    public void Test_GH61_IsCanonicalCells() {
        // Arrange
        var canonical = TestHelpers.SfIndex.GridDiskDistances(2)
            .Select(cell => cell.Index)
            .CanonicalizeCells();

        // Act
        var actual = canonical.IsCanonicalCells();

        // Assert
        Assert.That(actual, Is.True, "canonicalized set should be canonical");
        Assert.That(new List<H3Index> { canonical[1], canonical[0] }.IsCanonicalCells(), Is.False, "unsorted set should not be canonical");
        Assert.That(new List<H3Index> { canonical[0], canonical[0] }.IsCanonicalCells(), Is.False, "duplicated set should not be canonical");
        Assert.That(new List<H3Index> { H3Index.Invalid }.IsCanonicalCells(), Is.False, "set containing H3_NULL should not be canonical");
    }

    [Test]
    public void Test_GH61_CanonicalCellsContain_SameResolution() {
        // Arrange
        var disk = TestHelpers.SfIndex.GridDiskDistances(2).Select(cell => cell.Index).ToList();
        var canonical = disk.CanonicalizeCells();
        var outside = TestHelpers.SfIndex.GridDiskDistances(3)
            .Where(cell => cell.Distance == 3)
            .Select(cell => cell.Index);

        // Assert
        foreach (var cell in disk) {
            Assert.That(canonical.CanonicalCellsContain(cell), Is.True, $"should contain {cell}");
        }

        foreach (var cell in outside) {
            Assert.That(canonical.CanonicalCellsContain(cell), Is.False, $"should not contain {cell}");
        }
    }

    [Test]
    public void Test_GH61_CanonicalCellsContain_CompactedMixedResolutions() {
        // Arrange
        var expanded = Sunnyvale.GridDiskDistances(9).Select(cell => cell.Index).ToList();
        var canonical = expanded.CompactCells().CanonicalizeCells();

        // Assert
        foreach (var cell in expanded) {
            Assert.That(canonical.CanonicalCellsContain(cell), Is.True, $"compacted coverage should contain {cell}");
        }

        foreach (var child in expanded[0].GetChildrenForResolution(expanded[0].Resolution + 1)) {
            Assert.That(canonical.CanonicalCellsContain(child), Is.True, $"compacted coverage should contain descendant {child}");
        }

        Assert.That(canonical.CanonicalCellsContain(H3Index.Invalid), Is.False, "should not contain H3_NULL");
        Assert.That(canonical.CanonicalCellsContain(H3Index.Create(0, 4, 0)), Is.False, "should not contain a cell outside of the coverage");
    }

    [Test]
    public void Test_Upstream_Compact_Uncompactable() {
        // Act
        var actual = Uncompactable.CompactCells().ToList();

        // Assert
        Assert.That(actual, Is.EquivalentTo(Uncompactable), "should return original input");
    }

    [Test]
    public void Test_Upstream_Compact_UncompactableWithZero() {
        // Arrange
        var expected = UncompactableWithZero.Where(i => i != H3Index.Invalid).ToList();

        // Act
        var actual = UncompactableWithZero.CompactCells().ToList();

        // Assert
        Assert.That(actual, Is.EquivalentTo(expected), "should return original input without H3_NULL");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(16)]
    public void Test_Upstream_Uncompact_WrongResolution(int resolution) {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => UncompactSomeHexagons.UncompactCells(resolution).ToList());

        // Assert
        Assert.That(exception.Message, Is.EqualTo("set contains cell smaller than target resolution"), "expected message");
    }

    [Test]
    [TestCase(4)]
    [TestCase(5)]
    public void Test_Upstream_Uncompact_SomeHexagonAndPentagon(int baseCellNumber) {
        // Arrange
        var index = H3Index.Create(1, baseCellNumber, 0);
        var indexes = new[] { index };
        var expectedChildren = index.GetChildrenForResolution(2);

        // Act
        var actual = indexes.UncompactCells(2);

        // Assert
        Assert.That(actual, Is.EqualTo(expectedChildren), "should be equal");
    }

    [Test]
    [TestCase(4)]
    [TestCase(5)]
    public void Test_Upstream_Compact_SomeHexagonAndPentagon(int baseCellNumber) {
        // Arrange
        var index = H3Index.Create(1, baseCellNumber, 0);
        var expectedIndexes = new[] { index };
        var children = index.GetChildrenForResolution(2);

        // Act
        var actual = children.CompactCells();

        // Assert
        Assert.That(actual, Is.EqualTo(expectedIndexes), "should be equal");
    }

    //[Test]
    //public void Test_Upstream_Canonicalize() {
    //    // Arrange

    //    // Act
    //    var actual = TestUncompactArray.Canonicalize();
    //}

}