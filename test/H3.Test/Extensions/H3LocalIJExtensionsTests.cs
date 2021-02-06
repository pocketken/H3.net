using System;
using H3.Extensions;
using H3.Model;
using NUnit.Framework;

namespace H3.Test.Extensions {

    [TestFixture]
    public class H3LocalIJExtensionsTests {

        public static readonly H3Index PentagonIndex = TestHelpers.CreateIndex(0, 4, 0);

        // result of select h3_experimental_h3_to_local_ij('8e48e1d7038d527'::h3index, '8e48e1d7038952f'::h3index)
        public static readonly CoordIJ TestLocalIJ = new CoordIJ(-247608, -153923);

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
            H3Index start = 0x8e48e1d7038d527;
            H3Index end = 0x8e48e1d7038952f;

            // Act
            var localIj = start.ToLocalIJ(end);

            // Assert
            Assert.IsTrue(localIj == TestLocalIJ, "should be equal");
        }

    }
}
