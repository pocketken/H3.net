using System.Collections.Generic;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NUnit.Framework;

namespace H3.Test.Extensions {

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class H3HierarchyExtensionsTests {
        private static readonly H3Index BaseCell0 = H3Index.Create(0, 0, 0);

        [Test]
        public void Test_Upstream_GetParentForResolution() {
            // Arrange
            var fromCenter = Enumerable.Range(0, MAX_H3_RES + 1)
                .ToDictionary(res => res, res => H3Index.FromGeoCoord(TestHelpers.SfCoord, res));
            var sfAt15 = fromCenter[15];

            // Act
            var parents = Enumerable.Range(1, MAX_H3_RES + 1)
                .Select(res => sfAt15.GetParentForResolution(res - 1))
                .ToArray();

            // Assert
            foreach (var parent in parents) {
                Assert.AreEqual(fromCenter[parent.Resolution], parent, "should be equal");
            }
        }

        [Test]
        [TestCase(-1)]
        [TestCase(17)]
        [TestCase(10)]
        public void Test_Upstream_GetParentForResolution_FailsOnInvalidResolution(int resolution) {
            // Act
            var actual = TestHelpers.SfIndex.GetParentForResolution(resolution);

            // Assert
            Assert.AreEqual(H3Index.Invalid, actual, "should be H3_NULL");
        }

        [Test]
        public void Test_Upstream_GetParentForResolution_ReturnsSelfAtSameResolution() {
            // Act
            var actual = TestHelpers.SfIndex.GetParentForResolution(TestHelpers.SfIndex.Resolution);

            // Assert
            Assert.AreEqual(TestHelpers.SfIndex, actual, "should return self");
        }

        [Test]
        public void Test_Upstream_GetChildrenForResolution_OneResStep() {
            // Arrange
            var sfHex8 = H3Index.FromGeoCoord(TestHelpers.SfCoord, 8);
            var center = sfHex8.ToGeoCoord();
            var verts = sfHex8.GetCellBoundaryVertices().ToArray();

            // Act
            var children = sfHex8.GetChildrenForResolution(9).ToArray();

            // Assert
            Assert.AreEqual(7, children.Length, "should return 7 children");
            Assert.IsNotEmpty(children.Where(child => child == TestHelpers.SfIndex), "should contain sf @ 9");
            for (var i = 0; i < verts.Length; i += 1) {
                GeoCoord avg = (
                    (verts[i].Latitude + center.Latitude) / 2,
                    (verts[i].Longitude + center.Longitude) / 2
                );
                var avgIndex = H3Index.FromGeoCoord(avg, 9);
                Assert.IsNotEmpty(children.Where(child => child == avgIndex), $"unable to find expected child {avgIndex:x}");
            }
        }

        [Test]
        public void Test_Upstream_GetChildrenForResolution_MultipleResStep() {
            // Arrange
            var sfHex8 = H3Index.FromGeoCoord(TestHelpers.SfCoord, 8);

            // Act
            var children = sfHex8.GetChildrenForResolution(10);

            // Assert
            AssertDistinctChildCount(children, 49);
        }

        [Test]
        public void Test_Upstream_GetChildrenForResolution_Pentagon() {
            // Arrange
            var index = H3Index.Create(1, 4, 0);

            // Act
            var children = index.GetChildrenForResolution(3);

            // Assert
            AssertDistinctChildCount(children, 5 * 7 + 6);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(17)]
        [TestCase(8)]
        public void Test_Upstream_GetChildrenForResolution_FailsOnInvalidResolution(int resolution) {
            // Act
            var actual = TestHelpers.SfIndex.GetChildrenForResolution(resolution);

            // Assert
            Assert.IsEmpty(actual, "should return empty iterator");
        }

        [Test]
        public void Test_Upstream_GetChildrenForResolution_ReturnsSelfAtSomeResolution() {
            // Act
            var actual = TestHelpers.SfIndex.GetChildrenForResolution(TestHelpers.SfIndex.Resolution).ToArray();

            // Assert
            Assert.AreEqual(1, actual.Length, "should return 1 entry");
            Assert.AreEqual(TestHelpers.SfIndex, actual[0], "should return self");
        }

        [Test]
        public void Test_GetChildrenForResolution_TestIndexValue() {
            // Arrange
            H3Index h3 = new(TestHelpers.TestIndexValue);

            // Act
            var children = h3.GetChildrenForResolution(15).ToArray();

            // Assert
            TestHelpers.AssertAll(TestHelpers.TestIndexChildrenAtRes15, children);
        }

        [Test]
        public void Test_Upstream_IsNeighbour_NotANeighbourOfThyself() {
            // Act
            var actual = TestHelpers.SfIndex.IsNeighbour(TestHelpers.SfIndex);

            // Assert
            Assert.IsFalse(actual, "should not be a neighbour of itself");
        }

        [Test]
        public void Test_Upstream_GetChildCenterForResolution() {
            // Arrange
            var center = H3Index.Create(8, 4, Direction.J).ToGeoCoord();
            var indexes = Enumerable.Range(0, MAX_H3_RES)
                .Select(res => H3Index.FromGeoCoord(center, res));
            var centers = indexes.ToDictionary(i => i, i => H3Index.FromGeoCoord(i.ToGeoCoord(), i.Resolution + 1));

            // Act
            var children = indexes.ToDictionary(i => i, i => i.GetChildCenterForResolution(i.Resolution + 1));

            // Assert
            foreach (var index in indexes) {
                var child = children[index];
                Assert.AreEqual(centers[index], child, "should be equal");
                Assert.AreEqual(index.Resolution + 1, child.Resolution, "should be equal");
                Assert.AreEqual(index, child.GetParentForResolution(index.Resolution), "should be equal");
            }
        }

        [Test]
        public void Test_Upstream_GetChildCenterForResolution_SameResReturnsSelf() {
            // Act
            var actual = TestHelpers.SfIndex.GetChildCenterForResolution(TestHelpers.SfIndex.Resolution);

            // Assert
            Assert.AreEqual(TestHelpers.SfIndex, actual, "should return self for same resolution");
        }

        [Test]
        [TestCase(8)]
        [TestCase(-1)]
        [TestCase(17)]
        public void Test_Upstream_GetChildCenterForResolution_InvalidInputs(int resolution) {
            // Act
            var actual = TestHelpers.SfIndex.GetChildCenterForResolution(resolution);

            // Assert
            Assert.AreEqual(H3Index.Invalid, actual, "should return H3_NULL");
        }

        [Test]
        [TestCase(Direction.Center, 0, 0)]
        [TestCase(Direction.K, 1, 5)]
        [TestCase(Direction.J, 5, 0)]
        [TestCase(Direction.JK, 2, 0)]
        [TestCase(Direction.I, 4, 1)]
        [TestCase(Direction.IK, 3, 5)]
        [TestCase(Direction.IJ, 8, 1)]
        public void Test_GetDirectNeighbour_BaseCells(Direction direction, int expectedBaseCell, int baseRotations) {
            // Arrange
            var expectedRotations = BaseCells.Cells[expectedBaseCell].IsPentagon ? baseRotations + 1 : baseRotations;

            // Act
            var (actual, rotations) = BaseCell0.GetDirectNeighbour(direction);

            // Assert
            Assert.AreEqual(expectedBaseCell, actual.BaseCellNumber, $"should be {expectedBaseCell}");
            Assert.AreEqual(expectedRotations, rotations, $"{actual.BaseCellNumber} should be {expectedRotations} rotations from {expectedBaseCell}");
        }

        [Test]
        public void Test_Upstream_IsNeighbour_MatchesRing1() {
            // Arrange
            var neighbours = TestHelpers.SfIndex.GetKRing(1)
                .Where(cell => cell.Distance > 0)
                .ToArray();

            // Act
            var actual = neighbours
                .Where(cell => TestHelpers.SfIndex.IsNeighbour(cell.Index))
                .ToArray();

            // Assert
            Assert.AreEqual(neighbours.Length, actual.Length, "should all be neighbours");
        }

        [Test]
        public void Test_Upstream_IsNeighbour_DoesNotMatchRing2() {
            // Arrange
            var neighbours = TestHelpers.SfIndex.GetKRing(2)
                .Where(cell => cell.Distance > 1)
                .ToArray();

            // Act
            var actual = neighbours
                .Where(cell => TestHelpers.SfIndex.IsNeighbour(cell.Index))
                .ToArray();

            // Assert
            Assert.AreEqual(0, actual.Length, "should not be neighbours");
        }

        [Test]
        public void Test_Upstream_IsNeighbour_FalseOnInvalid() {
            // Arrange
            H3Index index = new(TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1) {
                Mode = Mode.UniEdge
            };

            // Act
            var actual = TestHelpers.SfIndex.IsNeighbour(index);

            // Assert
            Assert.IsFalse(actual, "invalid indexes should not be neighbours");
        }

        [Test]
        public void Test_Upstream_IsNeighbour_FalseOnResolutionDifference() {
            // Arrange
            H3Index index = new(TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1);

            // Act
            var actual = TestHelpers.SfIndex.IsNeighbour(index.GetParentForResolution(7));

            // Assert
            Assert.IsFalse(actual, "should not be neighbours if resolution differs");
        }

        private static void AssertDistinctChildCount(IEnumerable<H3Index> indicies, int expectedCount) {
            var groupCounts = indicies.GroupBy(i => i).Select(g => g.Count()).ToArray();
            Assert.IsEmpty(groupCounts.Where(count => count > 1), "should not contain duplicates");
            Assert.AreEqual(groupCounts.Length, expectedCount, $"should contain {expectedCount} children");
        }
    }

}
