using System.Linq;
using H3.Model;
using NUnit.Framework;

namespace H3.Test {

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class CoordIJKTests {

        [Test]
        [TestCase(0, 0, 0, Direction.Center)]
        [TestCase(0, 0, 1, Direction.K)]
        [TestCase(0, 1, 0, Direction.J)]
        [TestCase(0, 1, 1, Direction.JK)]
        [TestCase(1, 0, 0, Direction.I)]
        [TestCase(1, 0, 1, Direction.IK)]
        [TestCase(1, 1, 0, Direction.IJ)]
        [TestCase(2, 2, 2, Direction.Center)]
        [TestCase(2, 2, 3, Direction.K)]
        [TestCase(8, 1, 8, Direction.Invalid)]
        public void Test_CoordIJK_UnitVector_Matching(int i, int j, int k, Direction expectedIndex) {
            // Arrange
            CoordIJK coord = new(i, j, k);

            // Act
            var cell = (Direction)coord;

            // Assert
            Assert.AreEqual(expectedIndex, cell);
        }

        [Test]
        [TestCase(0, 1, 0, 2)]
        public void Test_CoordIJK_Equals(int i, int j, int k, int expectedIndex) {
            // Arrange
            CoordIJK coord = new(i, j, k);

            // Act
            var result = coord == LookupTables.UnitVectors[expectedIndex];

            // Assert
            Assert.IsTrue(result, "should be equal");
        }

        [Test]
        public void Test_CoordIJK_Addition() {
            // Arrange
            CoordIJK a = new(1, 1, 0);
            CoordIJK b = new(0, 1, 1);

            // Act
            var result = a + b;

            // Assert
            Assert.AreEqual(1, result.I, "I should be 1");
            Assert.AreEqual(2, result.J, "J should be 2");
            Assert.AreEqual(1, result.K, "K should be 1");
        }

        [Test]
        public void Test_CoordIJK_Subtraction() {
            // Arrange
            CoordIJK a = new(1, 1, 0);
            CoordIJK b = new(0, 1, 1);

            // Act
            var result = a - b;

            // Assert
            Assert.AreEqual(1, result.I, "I should be 1");
            Assert.AreEqual(0, result.J, "J should be 0");
            Assert.AreEqual(-1, result.K, "K should be -1");
        }

        [Test]
        public void Test_CoordIJK_Scaling() {
            // Arrange
            CoordIJK a = new(1, 1, 0);

            // Act
            var result = a *= 2;

            // Assert
            Assert.AreEqual(2, result.I, "I should be 2");
            Assert.AreEqual(2, result.J, "J should be 2");
            Assert.AreEqual(0, result.K, "K should be 0");
        }

        [Test]
        public void Test_CoordIJK_MultipleTransforms() {
            // Arrange
            CoordIJK a = new(1, 1, 0);
            CoordIJK b = new(0, 1, 1);
            CoordIJK c = new(1, 0, 0);

            // Act
            var result = (a + b + c) * 2;

            // Assert
            Assert.AreEqual(4, result.I, "I should be 4");
            Assert.AreEqual(4, result.J, "J should be 4");
            Assert.AreEqual(2, result.K, "K should be 2");
        }

        [Test]
        public void Test_CoordIJK_Normalize() {
            // Arrange
            CoordIJK a = new(-2, 2, 0);

            // Act
            var result = CoordIJK.Normalize(a);

            // Assert
            Assert.AreEqual(0, result.I, "I should be 0");
            Assert.AreEqual(4, result.J, "J should be 4");
            Assert.AreEqual(2, result.K, "K should be 2");
        }

        [Test]
        [TestCase(Direction.Center, 0, 0, 0)]
        [TestCase(Direction.I, 1, 0, 0)]
        [TestCase(Direction.Invalid, 0, 0, 0)]
        public void Test_CoordIJK_ToNeighbour(Direction direction, int expectedI, int expectedJ, int expectedK) {
            // Arrange
            CoordIJK ijk = new();

            // Act
            var neighbour = ijk.ToNeighbour(direction);

            // Assert
            Assert.AreEqual(expectedI, neighbour.I, $"I should be {expectedI}");
            Assert.AreEqual(expectedJ, neighbour.J, $"J should be {expectedJ}");
            Assert.AreEqual(expectedK, neighbour.K, $"K should be {expectedK}");
        }

        [Test]
        public void Test_Upstream_CubeUncube_Roundtrip() {
            // Arrange
            var coords = Enumerable.Range((int)Direction.Center, (int)Direction.Invalid)
                .Select(dir => new CoordIJK().ToNeighbour((Direction)dir));

            // Act
            var actual = coords.Select(ijk => ijk.Cube().Uncube());

            // Assert
            Assert.AreEqual(coords, actual, "should be equal");
        }

    }
}
