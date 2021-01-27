using System;
using H3.Model;
using NUnit.Framework;

namespace H3.Test {

    [TestFixture]
    public class CoordIJKTests {

        [Test]
        [TestCase(0, 0, 0, CellIndex.Center)]
        [TestCase(0, 0, 1, CellIndex.K)]
        [TestCase(0, 1, 0, CellIndex.J)]
        [TestCase(0, 1, 1, CellIndex.JK)]
        [TestCase(1, 0, 0, CellIndex.I)]
        [TestCase(1, 0, 1, CellIndex.IK)]
        [TestCase(1, 1, 0, CellIndex.IJ)]
        [TestCase(2, 2, 2, CellIndex.Center)]
        [TestCase(2, 2, 3, CellIndex.K)]
        public void Test_CoordIJK_UnitVector_Matching(int i, int j, int k, CellIndex expectedIndex) {
            // Arrange
            CoordIJK coord = new CoordIJK(i, j, k);

            // Act
            var cell = (CellIndex)coord;

            // Assert
            Assert.AreEqual(expectedIndex, cell);
        }

        [Test]
        public void Test_CoordIJK_Addition() {
            // Arrange
            CoordIJK a = new CoordIJK(1, 1, 0);
            CoordIJK b = new CoordIJK(0, 1, 1);

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
            CoordIJK a = new CoordIJK(1, 1, 0);
            CoordIJK b = new CoordIJK(0, 1, 1);

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
            CoordIJK a = new CoordIJK(1, 1, 0);

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
            CoordIJK a = new CoordIJK(1, 1, 0);
            CoordIJK b = new CoordIJK(0, 1, 1);
            CoordIJK c = new CoordIJK(1, 0, 0);

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
            CoordIJK a = new CoordIJK(-2, 2, 0);

            // Act
            var result = CoordIJK.Normalize(a);

            // Assert
            Assert.AreEqual(0, result.I, "I should be 0");
            Assert.AreEqual(4, result.J, "J should be 4");
            Assert.AreEqual(2, result.K, "K should be 2");
        }

    }
}
