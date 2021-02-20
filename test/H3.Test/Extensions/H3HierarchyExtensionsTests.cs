using System;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using NUnit.Framework;

namespace H3.Test.Extensions {

    [TestFixture]
    public class H3HierarchyExtensionsTests {

        // TODO copy relevant tests from upstream

        [Test]
        public void Test_KnownIndexValue_Children() {
            // Arrange
            H3Index h3 = new(TestHelpers.TestIndexValue);

            // Act
            H3Index[] children = h3.GetChildrenAtResolution(15).ToArray();

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
            int rotations = 0;
            H3Index index = new(TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ, ref rotations)) {
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
            int rotations = 0;
            H3Index index = new(TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ, ref rotations));

            // Act
            var actual = TestHelpers.SfIndex.IsNeighbour(index.GetParentForResolution(7));

            // Assert
            Assert.IsFalse(actual, "should not be neighbours if resolution differs");
        }

    }

}
