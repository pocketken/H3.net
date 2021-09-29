using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NUnit.Framework;
using System.Collections.Generic;

namespace H3.Test.Extensions {

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
            var actual = origin.GetUnidirectionalEdge(destination);

            // Assert
            Assert.AreEqual(pentagonEdge, actual, "should be equal");
        }

        [Test]
        public void Test_Upstream_GetUnidirectionalEdge_FailsIfNotNeighbours() {
            // Arrange
            var outerRingIndex = TestHelpers.SfIndex.GetKRing(2)
                .Where(cell => cell.Distance > 1)
                .Select(cell => cell.Index)
                .First();

            // Act
            var edge = TestHelpers.SfIndex.GetUnidirectionalEdge(outerRingIndex);

            // Assert
            Assert.AreEqual(H3Index.Invalid, edge, "should fail to create edge for non-neighbouring indicies");
        }

        [Test]
        public void Test_Upstream_UnidirectionalEdgeIsValid() {
            // Arrange
            H3Index pentagonEdge = new(0x821c07fffffffff) {
                Mode = Mode.UniEdge,
                ReservedBits = (int)Direction.IJ
            };

            // Act
            var result = pentagonEdge.IsUnidirectionalEdgeValid();

            // Assert
            Assert.IsTrue(result, "should be valid");
        }

        [Test]
        public void Test_UnidirectionalEdgeIsVaid_FalseOnNonEdge() {
            // Act
            var result = TestHelpers.SfIndex.IsUnidirectionalEdgeValid();

            // Assert
            Assert.IsFalse(result, "should not be valid");
        }

        [Test]
        public void Test_Upstream_UnidirectionalEdgeIsValid_FalseOnCenterDirection() {
            // Arrange
            H3Index edge = new(TestHelpers.SfIndex) {
                Mode = Mode.UniEdge,
                ReservedBits = (int)Direction.Center
            };

            // Act
            var result = edge.IsUnidirectionalEdgeValid();

            // Assert
            Assert.IsFalse(result, "should not be valid");
        }

        [Test]
        public void Test_Upstream_UnidirectionalEdgeIsValid_FalseOnInvalidDirection() {
            // Arrange
            H3Index edge = new(TestHelpers.SfIndex) {
                Mode = Mode.UniEdge,
                ReservedBits = (int)Direction.Invalid
            };

            // Act
            var result = edge.IsUnidirectionalEdgeValid();

            // Assert
            Assert.IsFalse(result, "should not be valid");
        }

        [Test]
        public void Test_Upstream_UnidirectionalEdgeIsValid_FalseOnInvalidDirection_Pentagon() {
            // Arrange
            H3Index pentagonEdge = new(0x821c07fffffffff) {
                Mode = Mode.UniEdge,
                ReservedBits = (int)Direction.K
            };

            // Act
            var result = pentagonEdge.IsUnidirectionalEdgeValid();

            // Assert
            Assert.IsFalse(result, "should not be valid");
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
            var result = pentagonEdge.IsUnidirectionalEdgeValid();

            // Assert
            Assert.IsFalse(result, "should not be valid");
        }

        [Test]
        public void Test_Upstream_GetOriginFromUnidirectionalEdge() {
            // Arrange
            var sf2 = TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1;
            var edge = TestHelpers.SfIndex.GetUnidirectionalEdge(sf2);

            // Act
            var origin = edge.GetOriginFromUnidirectionalEdge();

            // Assert
            Assert.AreEqual(TestHelpers.SfIndex, origin, "should be equal");
        }

        [Test]
        public void Test_Upstream_GetOriginFromUnidirectionalEdge_FailsOnNull() {
            // Act
            var origin = H3Index.Invalid.GetOriginFromUnidirectionalEdge();

            // Assert
            Assert.AreEqual(H3Index.Invalid, origin, "should not be valid");
        }

        [Test]
        public void Test_Upstream_GetOriginFromUnidirectionalEdge_FailsOnNonEdge() {
            // Act
            var origin = TestHelpers.SfIndex.GetOriginFromUnidirectionalEdge();

            // Assert
            Assert.AreEqual(H3Index.Invalid, origin, "should not be valid");
        }

        [Test]
        public void Test_Upstream_GetDestinationFromUnidirectionalEdge() {
            // Arrange
            var sf2 = TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1;
            var edge = TestHelpers.SfIndex.GetUnidirectionalEdge(sf2);

            // Act
            var destination = edge.GetDestinationFromUnidirectionalEdge();

            // Assert
            Assert.AreEqual(sf2, destination, "should be equal");
        }

        [Test]
        public void Test_Upstream_GetDestinationFromUnidirectionalEdge_FailsOnNull() {
            // Act
            var destination = H3Index.Invalid.GetOriginFromUnidirectionalEdge();

            // Assert
            Assert.AreEqual(H3Index.Invalid, destination, "should not be valid");
        }

        [Test]
        public void Test_Upstream_GetDestinationFromUnidirectionalEdge_FailsOnNonEdge() {
            // Act
            var destination = TestHelpers.SfIndex.GetDestinationFromUnidirectionalEdge();

            // Assert
            Assert.AreEqual(H3Index.Invalid, destination, "should not be valid");
        }

        [Test]
        public void Test_Upstream_GetIndexesFromUnidirectionalEdge() {
            // Arrange
            var sf2 = TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1;
            var edge = TestHelpers.SfIndex.GetUnidirectionalEdge(sf2);

            // Act
            var actual = edge.GetIndexesFromUnidirectionalEdge();

            // Assert
            Assert.AreEqual(TestHelpers.SfIndex, actual.Item1, "should be equal");
            Assert.AreEqual(sf2, actual.Item2, "should be equal");
        }

        [Test]
        public void Test_Upstream_GetUnidirectionalEdges_Hexagon() {
            // Arrange
            var indexes = new H3Index[] { TestHelpers.TestIndexValue, TestHelpers.SfIndex };

            var rings = indexes.Select(index =>
                index.GetKRing(1)
                    .Where(cell => cell.Distance > 0 && cell.Index != H3Index.Invalid)
                    .Select(cell => cell.Index)).ToArray();

            // Act
            var edges = indexes
                .Select(index => index.GetUnidirectionalEdges())
                .ToArray();

            // Assert
            AssertAllEdges(indexes, rings, edges);
        }

        [Test]
        public void Test_Upstream_GetUnidirectionalEdges_Pentagon() {
            // Arrange
            var pentagons = LookupTables.PentagonIndexesPerResolution
                .SelectMany(e => e.Value)
                .ToArray();

            var rings = pentagons.Select(index =>
                index.GetKRing(1)
                    .Where(cell => cell.Distance > 0 && cell.Index != H3Index.Invalid)
                    .Select(cell => cell.Index)).ToArray();

            // Act
            var edges = pentagons
                .Select(index => index.GetUnidirectionalEdges().Where(edge => edge != H3Index.Invalid))
                .ToArray();

            // Assert
            AssertAllEdges(pentagons, rings, edges);
        }

        [Test]
        public void Test_Upstream_GetUnidirectionalEdgeBoundaryVertices() {
            // Arrange
            var indexes = Enumerable.Range(0, MAX_H3_RES + 1)
                .Select(res => H3Index.FromGeoCoord(TestHelpers.SfCoord, res));

            var edgesPerIndex = indexes.Select(index => index.GetUnidirectionalEdges());

            var boundsPerIndex = indexes.Select(index => index.GetCellBoundaryVertices().ToArray()).ToArray();

            // Act
            var vertsPerIndex = edgesPerIndex
                .Select(edges => edges
                    .Select(edge => edge.GetUnidirectionalEdgeBoundaryVertices().ToArray()).ToArray()
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
            var edgesPerIndex = indexes.Select(index => index.GetUnidirectionalEdges());
            var boundsPerIndex = indexes.Select(index => index.GetCellBoundaryVertices().ToArray()).ToArray();

            // Act
            var vertsPerIndex = edgesPerIndex
                .Select(edges => edges
                    .Select(edge => edge.GetUnidirectionalEdgeBoundaryVertices().ToArray()).ToArray()
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
            var edgesPerIndex = indexes.Select(index => index.GetUnidirectionalEdges());
            var boundsPerIndex = indexes.Select(index => index.GetCellBoundaryVertices().ToArray()).ToArray();

            // Act
            var vertsPerIndex = edgesPerIndex
                .Select(edges => edges
                    .Select(edge => edge.GetUnidirectionalEdgeBoundaryVertices().ToArray()).ToArray()
                ).ToArray();

            // Assert
            AssertAllVertices(boundsPerIndex, vertsPerIndex, PentagonClass2ExpectedVertices, 2, 1);
        }

        [Test]
        public void Test_Upstream_GetExactEdgeLengthInRadians_ZeroForInvalid() {
            // Act
            var actual = H3Index.Invalid.GetExactEdgeLengthInRadians();

            // Assert
            Assert.AreEqual(0.0, actual, "should be zero");
        }

        [Test]
        public void Test_Upstream_GetExactEdgeLengthInRadians_ZeroForNonEdge() {
            // Act
            var actual = TestHelpers.SfIndex.GetExactEdgeLengthInRadians();

            // Assert
            Assert.AreEqual(0.0, actual, "should be zero");
        }

        private static void AssertAllEdges(H3Index[] origins, IEnumerable<H3Index>[] rings, IEnumerable<H3Index>[] actualEdges) {
            for (var i = 0; i < rings.Length; i += 1) {
                var origin = origins[i];
                var neighbours = rings[i];
                var edges = actualEdges[i];

                foreach (var edge in edges) {
                    Assert.IsTrue(edge.IsUnidirectionalEdgeValid(), $"{edge} should be valid");
                    var (edgeOrigin, edgeDest) = edge.GetIndexesFromUnidirectionalEdge();
                    Assert.AreEqual(origin, edgeOrigin, "should be equal");
                    Assert.AreEqual(1, neighbours.Where(neighbour => neighbour == edgeDest).Count(), "should have one match");
                }
            }
        }

        private static void AssertAllVertices(GeoCoord[][] expectedVertices, GeoCoord[][][] actualVertices, int[,] vertexMap, int expectedVertexCount, int maxEmpty) {
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

                    Assert.AreEqual(expectedVertexCount, edgeVert.Length, $"should have {expectedVertexCount} vertices");

                    for (var j = 0; j < expectedVertexCount; j += 1) {
                        var expectedVert = expectedVerts[vertexMap[i, j]];
                        Assert.IsTrue(
                            expectedVert.AlmostEquals(edgeVert[j]),
                            $"should be equal: {edgeVert[j].Longitude},{edgeVert[j].Latitude} == {expectedVert.Longitude},{expectedVert.Latitude}");
                    }
                }
            }
        }

    }
}
