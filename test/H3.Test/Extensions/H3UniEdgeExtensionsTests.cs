using System;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NUnit.Framework;

using System.Collections.Generic;

namespace H3.Test.Extensions; 

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class H3UniEdgeExtensionsTests {

    private static readonly int[,] SfExpectedVertices = new int[6, 2] {
        {3, 4}, {1, 2}, {2, 3}, {5, 0}, {4, 5}, {0, 1}
    };

    private static readonly int[,] PentagonClass3ExpectedVertices = new int[6, 3] {
        {-1, -1, -1 }, {2, 3, 4}, {4, 5, 6}, {8, 9, 0}, {6, 7, 8}, {0, 1, 2}
    };

    private static readonly int[,] PentagonClass2ExpectedVertices = new int[6, 2] {
        {-1, -1}, {1, 2}, {2, 3}, {4, 0}, {3, 4}, {0, 1}
    };

    [Test]
    public void Test_GetUnidirectionalEdge() {
        // Arrange
        H3Index origin = 0x821c07fffffffff;
        H3Index pentagonEdge = new(origin) {
            Mode = Mode.UniEdge,
            ReservedBits = (int)Direction.IJ
        };
        var destination = origin.GetDirectNeighbour(Direction.IJ).Item1;

        // Act
        var actual = origin.ToDirectedEdge(destination);

        // Assert
        Assert.That(actual, Is.EqualTo(pentagonEdge), "should be equal");
    }

    [Test]
    public void Test_Upstream_GetUnidirectionalEdge_FailsIfNotNeighbours() {
        // Arrange
        var outerRingIndex = TestHelpers.SfIndex.GridDiskDistances(2)
            .Where(cell => cell.Distance > 1)
            .Select(cell => cell.Index)
            .First();

        // Act
        var edge = TestHelpers.SfIndex.ToDirectedEdge(outerRingIndex);

        // Assert
        Assert.That(edge, Is.EqualTo(H3Index.Invalid), "should fail to create edge for non-neighbouring indicies");
    }

    [Test]
    public void Test_Upstream_UnidirectionalEdgeIsValid() {
        // Arrange
        H3Index pentagonEdge = new(0x821c07fffffffff) {
            Mode = Mode.UniEdge,
            ReservedBits = (int)Direction.IJ
        };

        // Act
        var result = pentagonEdge.IsValidDirectedEdge();

        // Assert
        Assert.That(result, Is.True, "should be valid");
    }

    [Test]
    public void Test_UnidirectionalEdgeIsVaid_FalseOnNonEdge() {
        // Act
        var result = TestHelpers.SfIndex.IsValidDirectedEdge();

        // Assert
        Assert.That(result, Is.False, "should not be valid");
    }

    [Test]
    public void Test_Upstream_UnidirectionalEdgeIsValid_FalseOnCenterDirection() {
        // Arrange
        H3Index edge = new(TestHelpers.SfIndex) {
            Mode = Mode.UniEdge,
            ReservedBits = (int)Direction.Center
        };

        // Act
        var result = edge.IsValidDirectedEdge();

        // Assert
        Assert.That(result, Is.False, "should not be valid");
    }

    [Test]
    public void Test_Upstream_UnidirectionalEdgeIsValid_FalseOnInvalidDirection() {
        // Arrange
        H3Index edge = new(TestHelpers.SfIndex) {
            Mode = Mode.UniEdge,
            ReservedBits = (int)Direction.Invalid
        };

        // Act
        var result = edge.IsValidDirectedEdge();

        // Assert
        Assert.That(result, Is.False, "should not be valid");
    }

    [Test]
    public void Test_Upstream_UnidirectionalEdgeIsValid_FalseOnInvalidDirection_Pentagon() {
        // Arrange
        H3Index pentagonEdge = new(0x821c07fffffffff) {
            Mode = Mode.UniEdge,
            ReservedBits = (int)Direction.K
        };

        // Act
        var result = pentagonEdge.IsValidDirectedEdge();

        // Assert
        Assert.That(result, Is.False, "should not be valid");
    }

    [Test]
    public void Test_Upstream_UnidirectionalEdgeIsValid_FalseOnHighBit() {
        // Arrange
        H3Index pentagonEdge = new(0x821c07fffffffff) {
            Mode = Mode.UniEdge,
            ReservedBits = (int)Direction.IJ,
            HighBit = 1
        };

        // Act
        var result = pentagonEdge.IsValidDirectedEdge();

        // Assert
        Assert.That(result, Is.False, "should not be valid");
    }

    [Test]
    public void Test_Upstream_GetOriginFromUnidirectionalEdge() {
        // Arrange
        var sf2 = TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1;
        var edge = TestHelpers.SfIndex.ToDirectedEdge(sf2);

        // Act
        var origin = edge.GetDirectedEdgeOrigin();

        // Assert
        Assert.That(origin, Is.EqualTo(TestHelpers.SfIndex), "should be equal");
    }

    [Test]
    public void Test_Upstream_GetOriginFromUnidirectionalEdge_FailsOnNull() {
        // Act
        var origin = H3Index.Invalid.GetDirectedEdgeOrigin();

        // Assert
        Assert.That(origin, Is.EqualTo(H3Index.Invalid), "should not be valid");
    }

    [Test]
    public void Test_Upstream_GetOriginFromUnidirectionalEdge_FailsOnNonEdge() {
        // Act
        var origin = TestHelpers.SfIndex.GetDirectedEdgeOrigin();

        // Assert
        Assert.That(origin, Is.EqualTo(H3Index.Invalid), "should not be valid");
    }

    [Test]
    public void Test_Upstream_GetDestinationFromUnidirectionalEdge() {
        // Arrange
        var sf2 = TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1;
        var edge = TestHelpers.SfIndex.ToDirectedEdge(sf2);

        // Act
        var destination = edge.GetDirectedEdgeDestination();

        // Assert
        Assert.That(destination, Is.EqualTo(sf2), "should be equal");
    }

    [Test]
    public void Test_Upstream_GetDestinationFromUnidirectionalEdge_FailsOnNull() {
        // Act
        var destination = H3Index.Invalid.GetDirectedEdgeOrigin();

        // Assert
        Assert.That(destination, Is.EqualTo(H3Index.Invalid), "should not be valid");
    }

    [Test]
    public void Test_Upstream_GetDestinationFromUnidirectionalEdge_FailsOnNonEdge() {
        // Act
        var destination = TestHelpers.SfIndex.GetDirectedEdgeDestination();

        // Assert
        Assert.That(destination, Is.EqualTo(H3Index.Invalid), "should not be valid");
    }

    [Test]
    public void Test_Upstream_GetIndexesFromUnidirectionalEdge() {
        // Arrange
        var sf2 = TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1;
        var edge = TestHelpers.SfIndex.ToDirectedEdge(sf2);

        // Act
        var actual = edge.DirectedEdgeToCells();

        // Assert
        Assert.That(actual.Item1, Is.EqualTo(TestHelpers.SfIndex), "should be equal");
        Assert.That(actual.Item2, Is.EqualTo(sf2), "should be equal");
    }

    [Test]
    public void Test_Upstream_GetUnidirectionalEdges_Hexagon() {
        // Arrange
        var indexes = new H3Index[] { TestHelpers.TestIndexValue, TestHelpers.SfIndex };

        var rings = indexes.Select(index =>
            index.GridDiskDistances(1)
                .Where(cell => cell.Distance > 0 && cell.Index != H3Index.Invalid)
                .Select(cell => cell.Index)).ToArray();

        // Act
        var edges = indexes
            .Select(index => index.OriginToDirectedEdges())
            .ToArray();

        // Assert
        AssertAllEdges(indexes, rings, edges);
    }

    [Test]
    public void Test_Upstream_GetUnidirectionalEdges_Pentagon() {
        // Arrange
        var pentagons = LookupTables.PentagonIndexesPerResolution
            .SelectMany(e => e)
            .ToArray();

        var rings = pentagons.Select(index =>
            index.GridDiskDistances(1)
                .Where(cell => cell.Distance > 0 && cell.Index != H3Index.Invalid)
                .Select(cell => cell.Index)).ToArray();

        // Act
        var edges = pentagons
            .Select(index => index.OriginToDirectedEdges().Where(edge => edge != H3Index.Invalid))
            .ToArray();

        // Assert
        AssertAllEdges(pentagons, rings, edges);
    }

    [Test]
    public void Test_Upstream_GetUnidirectionalEdgeBoundaryVertices() {
        // Arrange
        var indexes = Enumerable.Range(0, MAX_H3_RES + 1)
            .Select(res => H3Index.FromLatLng(TestHelpers.SfCoord, res));

        var edgesPerIndex = indexes.Select(index => index.OriginToDirectedEdges());

        var boundsPerIndex = indexes.Select(index => index.GetCellBoundaryVertices().ToArray()).ToArray();

        // Act
        var vertsPerIndex = edgesPerIndex
            .Select(edges => edges
                .Select(edge => edge.GetDirectedEdgeBoundaryVertices().ToArray()).ToArray()
            ).ToArray();

        // Assert
        AssertAllVertices(boundsPerIndex, vertsPerIndex, SfExpectedVertices, 2, 0);
    }

    [Test]
    public void Test_Upstream_GetUnidirectionalEdgeBoundaryVertices_PentagonClass3() {
        // Arrange
        var indexes = new List<H3Index>();
        for (var r = 1; r < MAX_H3_RES; r += 2) {
            indexes.Add(H3Index.Create(r, 24, 0));
        }
        var edgesPerIndex = indexes.Select(index => index.OriginToDirectedEdges());
        var boundsPerIndex = indexes.Select(index => index.GetCellBoundaryVertices().ToArray()).ToArray();

        // Act
        var vertsPerIndex = edgesPerIndex
            .Select(edges => edges
                .Select(edge => edge == H3Index.Invalid
                    ? Array.Empty<LatLng>()
                    : edge.GetDirectedEdgeBoundaryVertices().ToArray()).ToArray()
            ).ToArray();

        // Assert
        AssertAllVertices(boundsPerIndex, vertsPerIndex, PentagonClass3ExpectedVertices, 3, 1);
    }

    [Test]
    public void Test_Upstream_GetUnidirectionalEdgeBoundaryVertices_PentagonClass2() {
        // Arrange
        var indexes = new List<H3Index>();
        for (var r = 0; r < MAX_H3_RES; r += 2) {
            indexes.Add(H3Index.Create(r, 24, 0));
        }
        var edgesPerIndex = indexes.Select(index => index.OriginToDirectedEdges());
        var boundsPerIndex = indexes.Select(index => index.GetCellBoundaryVertices().ToArray()).ToArray();

        // Act
        var vertsPerIndex = edgesPerIndex
            .Select(edges => edges
                .Select(edge => edge == H3Index.Invalid
                    ? Array.Empty<LatLng>()
                    : edge.GetDirectedEdgeBoundaryVertices().ToArray()).ToArray()
            ).ToArray();

        // Assert
        AssertAllVertices(boundsPerIndex, vertsPerIndex, PentagonClass2ExpectedVertices, 2, 1);
    }

    [Test]
    public void Test_Upstream_GetExactEdgeLengthInRadians_ThrowsForInvalid() {
        // Act
        Action actual = () => H3Index.Invalid.EdgeLengthRadians();

        // Assert
        Assert.Throws<ArgumentException>(actual, "should throw for invalid index");
    }

    [Test]
    public void Test_GH109_EdgeLength_ThrowsForNonEdge() {
        // Arrange
        var index = H3Index.FromLatLng(new LatLng(-23.553301290491326 * M_PI_180, -46.65526874921591 * M_PI_180), 9);

        // Act
        Action actual = () => index.EdgeLengthMeters();

        // Assert
        Assert.Throws<ArgumentException>(actual, "should throw for cell index input");
    }

    [Test]
    public void Test_EdgeLength_ApproximatesAverageHexagonEdgeLength() {
        // Arrange
        var edges = TestHelpers.SfIndex.OriginToDirectedEdges();

        // Act
        var lengths = edges.Select(edge => edge.EdgeLengthMeters()).ToArray();

        // Assert
        var average = H3Index.GetHexagonEdgeLengthAverageInM(TestHelpers.SfIndex.Resolution);
        foreach (var length in lengths) {
            Assert.That(length > 0, Is.True, "should be positive");
            Assert.That(Math.Abs(length - average) / average < 0.25, Is.True, $"{length} should be within 25% of the resolution average {average}");
        }
    }

    [Test]
    public void Test_ReverseDirectedEdge_SwapsOriginAndDestination() {
        // Arrange
        var origin = TestHelpers.SfIndex;
        var destination = origin.GetDirectNeighbour(Direction.I).Item1;
        var edge = origin.ToDirectedEdge(destination);

        // Act
        var reversed = edge.ReverseDirectedEdge();

        // Assert
        Assert.That(reversed.IsValidDirectedEdge(), Is.True, "should be a valid directed edge");
        Assert.That(reversed.DirectedEdgeToCells(), Is.EqualTo((destination, origin)), "origin/destination should be swapped");
    }

    [Test]
    public void Test_ReverseDirectedEdge_RoundTrips() {
        // Arrange
        var origin = TestHelpers.SfIndex;
        var edge = origin.ToDirectedEdge(origin.GetDirectNeighbour(Direction.I).Item1);

        // Act
        var actual = edge.ReverseDirectedEdge().ReverseDirectedEdge();

        // Assert
        Assert.That(actual, Is.EqualTo(edge), "double reverse should round-trip");
    }

    [Test]
    public void Test_ReverseDirectedEdge_InvalidForInvalidEdge() {
        // Act
        var actual = H3Index.Invalid.ReverseDirectedEdge();

        // Assert
        Assert.That(actual, Is.EqualTo(H3Index.Invalid), "reverse of invalid should be invalid");
    }

    [Test]
    public void Test_DestinationToDirectedEdges_AllPointAtDestination() {
        // Arrange
        var destination = TestHelpers.SfIndex;

        // Act
        var edges = destination.DestinationToDirectedEdges().ToArray();

        // Assert
        Assert.That(edges.Length, Is.EqualTo(6), "should produce six edges");
        foreach (var edge in edges) {
            Assert.That(edge.IsValidDirectedEdge(), Is.True, $"{edge} should be valid");
            Assert.That(edge.GetDirectedEdgeDestination(), Is.EqualTo(destination), "should point at destination");
        }
    }

    [Test]
    public void Test_DestinationToDirectedEdges_PentagonYieldsInvalidPlaceholder() {
        // Arrange
        var pentagon = H3Index.Create(2, 4, 0);

        // Act
        var edges = pentagon.DestinationToDirectedEdges().ToArray();

        // Assert
        Assert.That(edges[0], Is.EqualTo(H3Index.Invalid), "K direction should be invalid for pentagons");
        Assert.That(edges.Count(edge => edge.IsValidDirectedEdge()), Is.EqualTo(5), "should produce five valid edges");
    }

    private static void AssertAllEdges(H3Index[] origins, IEnumerable<H3Index>[] rings, IEnumerable<H3Index>[] actualEdges) {
        for (var i = 0; i < rings.Length; i += 1) {
            var origin = origins[i];
            var neighbours = rings[i];
            var edges = actualEdges[i];

            foreach (var edge in edges) {
                Assert.That(edge.IsValidDirectedEdge(), Is.True, $"{edge} should be valid");
                var (edgeOrigin, edgeDest) = edge.DirectedEdgeToCells();
                Assert.That(edgeOrigin, Is.EqualTo(origin), "should be equal");
                Assert.That(neighbours.Where(neighbour => neighbour == edgeDest).Count(), Is.EqualTo(1), "should have one match");
            }
        }
    }

    private static void AssertAllVertices(LatLng[][] expectedVertices, LatLng[][][] actualVertices, int[,] vertexMap, int expectedVertexCount, int maxEmpty) {
        for (var e = 0; e < actualVertices.Length; e += 1) {
            var empty = 0;
            var edgeVerts = actualVertices[e];
            var expectedVerts = expectedVertices[e];

            for (var i = 0; i < 6; i += 1) {
                var edgeVert = edgeVerts[i];
                if (edgeVert.Length == 0) {
                    empty += 1;
                    if (empty > maxEmpty) {
                        Assert.Fail($"should not contain more than {maxEmpty} empty set of vertexes");
                    }
                    continue;
                }

                Assert.That(edgeVert.Length, Is.EqualTo(expectedVertexCount), $"should have {expectedVertexCount} vertices");

                for (var j = 0; j < expectedVertexCount; j += 1) {
                    var expectedVert = expectedVerts[vertexMap[i, j]];
                    Assert.That(expectedVert.AlmostEquals(edgeVert[j]), Is.True, $"should be equal: {edgeVert[j].Longitude},{edgeVert[j].Latitude} == {expectedVert.Longitude},{expectedVert.Latitude}");
                }
            }
        }
    }

}