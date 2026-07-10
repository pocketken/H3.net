using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using NetTopologySuite.Geometries;
using NUnit.Framework;


namespace H3.Test.Algorithms;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LineTests {

    // result of select h3_line(h3_geo_to_h3(point(-110, 30), 14), h3_geo_to_h3(point(-110, 30.0005), 14));
    private static readonly H3Index[] TestLineIndicies = {
        0x8e48e1d7038d527,
        0x8e48e1d7038d507,
        0x8e48e1d7038d50f,
        0x8e48e1d7038d427,
        0x8e48e1d7038d407,
        0x8e48e1d7038d40f,
        0x8e48e1d7038d4e7,
        0x8e48e1d7038d4ef,
        0x8e48e1d7038d4cf,
        0x8e48e1d70388b67,
        0x8e48e1d70388b6f,
        0x8e48e1d70388b4f,
        0x8e48e1d70388a67,
        0x8e48e1d70388a6f,
        0x8e48e1d70388a4f,
        0x8e48e1d70389da7,
        0x8e48e1d70389daf,
        0x8e48e1d70389d8f,
        0x8e48e1d70389c17,
        0x8e48e1d70389caf,
        0x8e48e1d70389c8f,
        0x8e48e1d70389cd7,
        0x8e48e1d7038952f
    };

    [Test]
    public void Test_LineTo_ReturnsExpectedIndicies() {
        // Arrange
        var start = H3Index.FromPoint(new Point(-110, 30), 14);
        var end = H3Index.FromPoint(new Point(-110, 30.0005), 14);

        // Act
        var line = start.GridPathCells(end).ToArray();

        // Assert
        TestHelpers.AssertAll(TestLineIndicies, line);
    }

    [Test]
    public void Test_DistanceTo_FailsAcrossMultipleFaces() {
        // Arrange
        H3Index start = 0x85285aa7fffffff;
        H3Index end = 0x851d9b1bfffffff;

        // Act
        var lineSize = start.GridDistance(end);

        // Assert
        Assert.That(lineSize, Is.EqualTo(-1), "line size should be -1");
    }

    [Test]
    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 5)]
    public void Test_Upstream_LineTo_KRing_Assertions(int resolution, int k) {
        // Arrange
        var endpoints = TestHelpers.GetAllCellsForResolution(resolution)
            .Where(index => !index.IsPentagon)
            .SelectMany(start =>
                start
                    .GridDiskDistances(k)
                    .Select(n => (Start: start, End: n.Index, Distance: start.GridDistance(n.Index)))
            );

        // Act
        var lines = endpoints.Select(e => (e.Start, e.End, e.Distance, Line: e.Start.GridPathCells(e.End)));

        // Assert
        foreach (var (Start, End, Distance, Line) in lines) {
            if (Distance >= 0) {
                var i = 0;
                H3Index lastIndex = H3Index.Invalid;
                H3Index previousLastIndex = H3Index.Invalid;

                foreach (var index in Line) {
                    if (i == 0) {
                        Assert.That(index, Is.EqualTo(Start), $"line should start with {Start}");
                    }

                    Assert.That(index.IsValidCell, Is.True, $"{index} should be valid");
                    if (lastIndex != H3Index.Invalid) {
                        Assert.That(index.IsNeighbour(lastIndex), Is.True, $"{index} should be neighbours with previous index {lastIndex}");
                    }

                    if (previousLastIndex != H3Index.Invalid) {
                        Assert.That(index.IsNeighbour(previousLastIndex), Is.False, $"{index} should not be neighbours with index before previous {previousLastIndex}");
                    }

                    i++;
                    previousLastIndex = lastIndex;
                    lastIndex = index;
                }

                Assert.That(lastIndex, Is.EqualTo(End), $"line should end with {End}");
                Assert.That(i, Is.EqualTo(Distance + 1), $"line should have count of {Distance + 1}");
            } else {
                Assert.That(Line, Is.Empty, "should be empty for invalid distances");
            }
        }
    }

    [Test]
    public void Test_Upstream_GridPathCells_PentagonReverseInterpolation() {
        // Arrange
        H3Index start = 0x820807fffffffff;
        H3Index end = 0x8208e7fffffffff;
        var distance = start.GridDistance(end);

        // Act
        var path = start.GridPathCells(end).ToArray();

        // Assert
        Assert.That(path.Length, Is.EqualTo(distance + 1), $"path should contain {distance + 1} cells");
        Assert.That(path[0], Is.EqualTo(start), "path should start at the origin");
        Assert.That(path[path.Length - 1], Is.EqualTo(end), "path should end at the destination");
        for (var i = 1; i < path.Length; i += 1) {
            Assert.That(path[i].IsValidCell, Is.True, $"{path[i]} should be valid");
            Assert.That(path[i].IsNeighbour(path[i - 1]), Is.True, $"{path[i]} should neighbour {path[i - 1]}");
        }
    }

    [Test]
    public void Test_Upstream_GridPathCells_KnownFailureNotCoveredByReverseInterpolation() {
        // Arrange
        H3Index start = 0x8411b61ffffffff;
        H3Index end = 0x84016d3ffffffff;
        Assume.That(start.GridDistance(end) >= 0, "distance should be computable");

        // Act
        var path = start.GridPathCells(end);

        // Assert
        Assert.That(path, Is.Empty, "interpolation should fail in both anchor charts");
    }
}