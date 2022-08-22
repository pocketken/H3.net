using System.Collections.Generic;
using System.Linq;
using H3.Algorithms;
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
        var actual = new H3Index(TestHelpers.TestIndexValue).GetKRingSlow(2);

        // Assert
        AssertRing(TestHelpers.TestIndexKRingsTo2, actual.ToArray());
    }

    [Test]
    public void Test_GetKRingFast_KnownValue() {
        // Act
        var ringDistanceList = new H3Index(TestHelpers.TestIndexValue).GetKRingFast(2);

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
        var ring = index.GetKRing(1).ToArray();

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
        var ring = index.GetKRing(1).ToArray();

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
        var ring = index.GetKRing(1).ToArray();

        // Assert
        AssertRing(expected, ring);
    }

    [Test]
    public void Test_Upstream_GetHexRing_Identity() {
        // Act
        var actual = TestHelpers.SfIndex.GetHexRing(0).ToList();

        // Assert
        Assert.AreEqual(1, actual.Count, "should have count of 1");
        Assert.AreEqual(TestHelpers.SfIndex, actual[0], "should be equal");
    }

    [Test]
    [TestCaseSource(nameof(HexRingTestCases))]
    public void Test_Upstream_GetHexRing_Ring(int k, H3Index[] expectedRing) {
        // Act
        var actual = TestHelpers.SfIndex.GetHexRing(k).ToList();

        // Assert
        Assert.AreEqual(expectedRing.Length, actual.Count, "should have same count");
        for (var i = 0; i < expectedRing.Length; i += 1) {
            var expectedIndex = expectedRing[i];
            var actualIndex = actual[i];
            Assert.AreEqual(expectedIndex, actualIndex, "should be equal");
        }
    }

    [Test]
    [TestCase(1)]
    [TestCase(2)]
    public void Test_Upstream_GetHexRing_NearPentagon(int k) {
        // Arrange
        H3Index nearPentagon = 0x837405fffffffff;

        // Act
        var exception = Assert.Throws<HexRingPentagonException>(() => nearPentagon.GetHexRing(k).ToList(), "should throw pentagon exception");
    }

    [Test]
    public void Test_Upstream_GetHexRing_OnPentagon() {
        // Arrange
        var onPentagon = H3Index.Create(0, 4, 0);

        // Act
        var exception = Assert.Throws<HexRingPentagonException>(() => onPentagon.GetHexRing(2).ToList(), "should throw pentagon exception");

        // Assert
        Assert.That(exception, Is.Not.Null, "should have thrown exception");
    }

    [Test]
    public void Test_Upstream_372_GridDiskInvalidDigit() {
        // Arrange
        H3Index invalidDigit = 0x4d4b00fe5c5c3030;

        // Act
        var exception = Assert.Throws<HexRingKSequenceException>(() => invalidDigit.GetHexRing(2).First());

        // Assert
        Assert.That(exception, Is.Not.Null, "should have thrown exception");
    }

    private static void AssertRing((H3Index, int)[] expectedRing, RingCell[] actualRing) {
        Assert.AreEqual(expectedRing.Length, actualRing.Length, "should be same length");
        for (var i = 0; i < expectedRing.Length; i += 1) {
            var expected = expectedRing[i];

            Assert.IsNotNull(actualRing.FirstOrDefault(cell => cell.Index == expected.Item1 && cell.Distance == expected.Item2), $"can't find {expected.Item1:x} at k {expected.Item2}");
        }
    }

}