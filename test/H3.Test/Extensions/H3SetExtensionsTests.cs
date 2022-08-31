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
        Assert.AreEqual(73, actual.Count, "should reduce to 73 indexes");
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
        Assert.AreEqual(expectedCount, actual.Count, $"should return {expectedCount}");
    }

    [Test]
    public void Test_Upstream_Compact_Uncompactable() {
        // Act
        var actual = Uncompactable.CompactCells().ToList();

        // Assert
        Assert.AreEqual(Uncompactable, actual, "should return original input");
    }

    [Test]
    public void Test_Upstream_Compact_UncompactableWithZero() {
        // Arrange
        var expected = UncompactableWithZero.Where(i => i != H3Index.Invalid).ToList();

        // Act
        var actual = UncompactableWithZero.CompactCells().ToList();

        // Assert
        Assert.AreEqual(expected, actual, "should return original input without H3_NULL");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(16)]
    public void Test_Upstream_Uncompact_WrongResolution(int resolution) {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => UncompactSomeHexagons.UncompactCells(resolution).ToList());

        // Assert
        Assert.AreEqual("set contains cell smaller than target resolution", exception.Message, "expected message");
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
        Assert.AreEqual(expectedChildren, actual, "should be equal");
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
        Assert.AreEqual(expectedIndexes, actual, "should be equal");
    }

    //[Test]
    //public void Test_Upstream_Canonicalize() {
    //    // Arrange

    //    // Act
    //    var actual = TestUncompactArray.Canonicalize();
    //}

}