using System;
using System.Linq;
using H3.Model;
using NUnit.Framework;

namespace H3.Test.Model {

    [TestFixture]
    public class CoordIJTests {

        [Test]
        public void Test_Upstream_IJKToIJ_Zero() {
            // Arrange
            CoordIJK ijk = new();

            // Act
            var actual = CoordIJ.FromCoordIJK(ijk);

            // Assert
            Assert.AreEqual(0, actual.I, "should be zero");
            Assert.AreEqual(0, actual.J, "should be zero");
        }

        [Test]
        public void Test_Upstream_IJToIJK_Zero() {
            // Arrange
            CoordIJ ij = new();

            // Act
            var actual = ij.ToCoordIJK();

            // Assert
            Assert.AreEqual(0, actual.I, "should be zero");
            Assert.AreEqual(0, actual.J, "should be zero");
            Assert.AreEqual(0, actual.K, "should be zero");
        }

        [Test]
        public void Test_Upstream_IJKToIJ_Roundtrip() {
            // Arrange
            var coords = Enumerable.Range((int)Direction.Center, (int)Direction.Invalid)
                .Select(dir => new CoordIJK().ToNeighbour((Direction)dir));

            // Act
            var actual = coords.Select(ijk => CoordIJ.FromCoordIJK(ijk).ToCoordIJK());

            // Assert
            Assert.AreEqual(coords, actual, "should be equal");
        }

    }
}
