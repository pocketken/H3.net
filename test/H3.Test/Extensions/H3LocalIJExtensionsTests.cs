using System;
using H3.Extensions;
using H3.Model;
using NUnit.Framework;

namespace H3.Test.Extensions {

    [TestFixture]
    public class H3LocalIJExtensionsTests {

        public static readonly H3Index PentagonIndex = TestHelpers.CreateIndex(0, 4, 0);

        [Test]
        [TestCase(0, 15, Direction.Center)]
        public void Test_H3IndexToLocalIJK_BaseCell(int resolution, int baseCell, Direction direction) {
            // Arrange
            H3Index index = TestHelpers.CreateIndex(resolution, baseCell, direction);

            // Act
            var ijk = LocalCoordIJK.ToLocalIJK(PentagonIndex, index);

            // Assert
            Assert.IsTrue(ijk.IsValid, "should be valid");
            Assert.IsTrue(ijk == LookupTables.UnitVectors[2], "should be equal to UnitVectors[2]");
        }

        [Test]
        public void Test_H3IndexToLocalIJ_MatchesPg() {
            // Arrange
            H3Index start = 0x85285aa7fffffff;
            H3Index end = 0x851d9b1bfffffff;

            // Act
            var localIj = start.ToLocalIJ(end);

            // Assert
            Assert.AreEqual(64, localIj.I, "I should be 64");
            Assert.AreEqual(0, localIj.J, "J should be 0");
        }

    }
}
