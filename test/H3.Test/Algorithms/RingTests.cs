using System;
using System.Collections.Generic;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using NUnit.Framework;


namespace H3.Test.Algorithms; 

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class RingTests {

    private static readonly IEnumerable<object[]> HexRingTestCases = new List<object[]> {
        new object[] {
            1,
            new H3Index[] {
                0x89283080ddbffff, 0x89283080c37ffff,
                0x89283080c27ffff, 0x89283080d53ffff,
                0x89283080dcfffff, 0x89283080dc3ffff
            }
        },
        new object[] {
            2,
            new H3Index[] {
                0x89283080ca7ffff, 0x89283080cafffff, 0x89283080c33ffff,
                0x89283080c23ffff, 0x89283080c2fffff, 0x89283080d5bffff,
                0x89283080d43ffff, 0x89283080d57ffff, 0x89283080d1bffff,
                0x89283080dc7ffff, 0x89283080dd7ffff, 0x89283080dd3ffff
            }
        }
    };

    [Test]
    public void Test_GetKRingSlow_KnownValue() {
        // Act
        var actual = new H3Index(TestHelpers.TestIndexValue).GridDiskDistancesSafe(2);

        // Assert
        AssertRing(TestHelpers.TestIndexKRingsTo2, actual.ToArray());
    }

    [Test]
    public void Test_GetKRingFast_KnownValue() {
        // Act
        var ringDistanceList = new H3Index(TestHelpers.TestIndexValue).GridDiskDistancesUnsafe(2);

        // Assert
        AssertRing(TestHelpers.TestIndexKRingsTo2, ringDistanceList.ToArray());
    }

    [Test]
    public void Test_Upstream_GetKRing() {
        // Arrange
        var index = H3Index.FromLatLng((0.659966917655, 2 * 3.14159 - 2.1364398519396), 0);
        (H3Index, int)[] expected = {
            (0x8029fffffffffff, 0),
            (0x801dfffffffffff, 1),
            (0x8013fffffffffff, 1),
            (0x8027fffffffffff, 1),
            (0x8049fffffffffff, 1),
            (0x8051fffffffffff, 1),
            (0x8037fffffffffff, 1)
        };

        // Act
        var ring = index.GridDiskDistances(1).ToArray();

        // Assert
        AssertRing(expected, ring);
    }

    [Test]
    public void Test_Upstream_GetKRing_PolarPentagonRes0() {
        // Arrange
        var index = H3Index.Create(0, 4, 0);
        (H3Index, int)[] expected = {
            (0x8009fffffffffff, 0),
            (0x8007fffffffffff, 1),
            (0x8001fffffffffff, 1),
            (0x8011fffffffffff, 1),
            (0x801ffffffffffff, 1),
            (0x8019fffffffffff, 1),
        };

        // Act
        var ring = index.GridDiskDistances(1).ToArray();

        // Assert
        AssertRing(expected, ring);
    }

    [Test]
    public void Test_Upstream_GetKRing_PolarPentagonRes1() {
        // Arrange
        var index = H3Index.Create(1, 4, 0);
        (H3Index, int)[] expected = {
            (0x81083ffffffffff, 0),
            (0x81093ffffffffff, 1),
            (0x81097ffffffffff, 1),
            (0x8108fffffffffff, 1),
            (0x8108bffffffffff, 1),
            (0x8109bffffffffff, 1),
        };

        // Act
        var ring = index.GridDiskDistances(1).ToArray();

        // Assert
        AssertRing(expected, ring);
    }

    [Test]
    public void Test_Upstream_GetHexRing_Identity() {
        // Act
        var actual = TestHelpers.SfIndex.GridRing(0).ToList();

        // Assert
        Assert.That(actual.Count, Is.EqualTo(1), "should have count of 1");
        Assert.That(actual[0], Is.EqualTo(TestHelpers.SfIndex), "should be equal");
    }

    [Test]
    [TestCaseSource(nameof(HexRingTestCases))]
    public void Test_Upstream_GetHexRing_Ring(int k, H3Index[] expectedRing) {
        // Act
        var actual = TestHelpers.SfIndex.GridRing(k).ToList();

        // Assert
        Assert.That(actual.Count, Is.EqualTo(expectedRing.Length), "should have same count");
        for (var i = 0; i < expectedRing.Length; i += 1) {
            var expectedIndex = expectedRing[i];
            var actualIndex = actual[i];
            Assert.That(actualIndex, Is.EqualTo(expectedIndex), "should be equal");
        }
    }

    [Test]
    [TestCase(1)]
    [TestCase(2)]
    public void Test_Upstream_GetHexRing_NearPentagon(int k) {
        // Arrange
        H3Index nearPentagon = 0x837405fffffffff;

        // Act
        var exception = Assert.Throws<HexRingPentagonException>(() => nearPentagon.GridRingUnsafe(k).ToList(), "should throw pentagon exception");

        // Assert
        Assert.That(exception, Is.Not.Null, "should have thrown exception");
    }

    [Test]
    public void Test_Upstream_GetHexRing_OnPentagon() {
        // Arrange
        var onPentagon = H3Index.Create(0, 4, 0);

        // Act
        var exception = Assert.Throws<HexRingPentagonException>(() => onPentagon.GridRingUnsafe(2).ToList(), "should throw pentagon exception");

        // Assert
        Assert.That(exception, Is.Not.Null, "should have thrown exception");
    }

    [Test]
    public void Test_Upstream_372_GridDiskInvalidDigit() {
        // Arrange
        H3Index invalidDigit = 0x4d4b00fe5c5c3030;

        // Act
        var exception = Assert.Throws<HexRingKSequenceException>(() => invalidDigit.GridRingUnsafe(2).First());

        // Assert
        Assert.That(exception, Is.Not.Null, "should have thrown exception");
    }

    [Test]
    public void Test_Upstream_GridRing_InvalidCell() {
        // Arrange
        H3Index invalidDigit = 0x4d4b00fe5c5c3030;

        // Act
        Action actual = () => invalidDigit.GridRing(2);

        // Assert
        Assert.Throws<ArgumentException>(actual, "should throw for invalid cell");
    }

    [Test]
    public void Test_Upstream_GridRing_NegativeK() {
        // Act
        Action actual = () => TestHelpers.SfIndex.GridRing(-1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(actual, "should throw for negative k");
    }

    [Test]
    public void Test_Upstream_GridRing_Identity() {
        // Act
        var actual = TestHelpers.SfIndex.GridRing(0).ToArray();

        // Assert
        Assert.That(actual, Is.EqualTo(new[] { TestHelpers.SfIndex }), "should return only the origin");
    }

    [Test]
    public void Test_Upstream_GridRing0_PolarPentagon() {
        // Arrange
        var polar = H3Index.Create(0, 4, 0);
        H3Index[] expected = {
            0x8007fffffffffff, 0x8001fffffffffff, 0x8011fffffffffff,
            0x801ffffffffffff, 0x8019fffffffffff
        };

        // Act
        var actual = polar.GridRing(1).ToArray();

        // Assert
        Assert.That(actual, Is.EquivalentTo(expected), "should return the pentagon's 5 neighbours");
    }

    [Test]
    public void Test_Upstream_GridRing1_PolarPentagon() {
        // Arrange
        var polar = H3Index.Create(1, 4, 0);
        H3Index[] expected = {
            0x81093ffffffffff, 0x81097ffffffffff, 0x8108fffffffffff,
            0x8108bffffffffff, 0x8109bffffffffff
        };

        // Act
        var actual = polar.GridRing(1).ToArray();

        // Assert
        Assert.That(actual, Is.EquivalentTo(expected), "should return the pentagon's 5 neighbours");
    }

    [Test]
    public void Test_Upstream_GridRing1_PolarPentagon_K3() {
        // Arrange
        var polar = H3Index.Create(1, 4, 0);
        H3Index[] expected = {
            0x811fbffffffffff, 0x81003ffffffffff, 0x81183ffffffffff,
            0x8111bffffffffff, 0x81067ffffffffff, 0x811e7ffffffffff,
            0x8101bffffffffff, 0x81107ffffffffff, 0x81063ffffffffff,
            0x811e3ffffffffff, 0x8119bffffffffff, 0x81103ffffffffff,
            0x81007ffffffffff, 0x81187ffffffffff, 0x8107bffffffffff
        };

        // Act
        var actual = polar.GridRing(3).ToArray();

        // Assert
        Assert.That(actual, Is.EquivalentTo(expected), "should return 15 cells at ring 3");
    }

    [Test]
    public void Test_Upstream_GridRing1_Pentagon_K4() {
        // Arrange
        var pentagon = H3Index.Create(1, 14, 0);
        H3Index[] expected = {
            0x81227ffffffffff, 0x81293ffffffffff, 0x8136bffffffffff,
            0x81167ffffffffff, 0x81477ffffffffff, 0x810dbffffffffff,
            0x81473ffffffffff, 0x81237ffffffffff, 0x81127ffffffffff,
            0x8126bffffffffff, 0x81177ffffffffff, 0x810d3ffffffffff,
            0x8150fffffffffff, 0x8102fffffffffff, 0x8129bffffffffff,
            0x8102bffffffffff, 0x81507ffffffffff, 0x8136fffffffffff,
            0x8127bffffffffff, 0x81137ffffffffff
        };

        // Act
        var actual = pentagon.GridRing(4).ToArray();

        // Assert
        Assert.That(actual, Is.EquivalentTo(expected), "should return 20 cells at ring 4");
    }

    [Test]
    public void Test_Upstream_GridRing_MatchesGridDiskDistancesSafe([Range(0, 2)] int k) {
        // Arrange
        var cells = H3Index.GetRes0Cells()
            .SelectMany(cell => cell.GetChildrenForResolution(1))
            .Select(cell => (
                Cell: cell,
                Expected: cell.GridDiskDistancesSafe(k)
                    .Where(ringCell => ringCell.Distance == k)
                    .Select(ringCell => ringCell.Index)
                    .ToArray()
            ));

        foreach (var (cell, expected) in cells) {
            // Act
            var ring = cell.GridRing(k).ToArray();

            // Assert
            Assert.That(ring, Is.EquivalentTo(expected), $"ring {k} of {cell} should match filtered disk");
        }
    }

    private static void AssertRing((H3Index, int)[] expectedRing, RingCell[] actualRing) {
        Assert.That(actualRing.Length, Is.EqualTo(expectedRing.Length), "should be same length");
        for (var i = 0; i < expectedRing.Length; i += 1) {
            var expected = expectedRing[i];

            Assert.That(actualRing.FirstOrDefault(cell => cell.Index == expected.Item1 && cell.Distance == expected.Item2), Is.Not.Null, $"can't find {expected.Item1:x} at k {expected.Item2}");
        }
    }

}