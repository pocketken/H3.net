﻿using System;
using H3.Extensions;
using H3.Model;
using NUnit.Framework;

namespace H3.Test.Extensions {

    [TestFixture]
    public class H3LocalIJExtensionsTests {

        public static readonly H3Index BaseCell15 = H3Index.Create(0, 15, 0);
        public static readonly H3Index PentagonIndex = H3Index.Create(0, 4, 0);

        // result of select h3_experimental_h3_to_local_ij('8e48e1d7038d527'::h3index, '8e48e1d7038952f'::h3index)
        public static readonly CoordIJ TestLocalIJ = (-247608, -153923);

        [Test]
        [TestCase(0, 15, Direction.Center)]
        public void Test_H3IndexToLocalIJK_BaseCell(int resolution, int baseCell, Direction direction) {
            // Arrange
            H3Index index = H3Index.Create(resolution, baseCell, direction);

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

        [Test]
        public void Test_Upstream_ToLocalIJK_BaseCells() {
            // Act
            var actual = PentagonIndex.ToLocalIJK(BaseCell15);

            // Assert
            Assert.IsTrue(actual.IsValid, "should be valid");
            Assert.AreEqual(LookupTables.UnitVectors[2], actual, "should equal 0,1,0");
        }

        [Test]
        public void Test_Upstream_FromLocalIJ_BaseCells_OriginMatchesSelf() {
            // Arrange
            H3Index origin = 0x8029fffffffffff;
            CoordIJ zero = (0, 0);

            // Act
            var actual = origin.FromLocalIJ(zero);

            // Assert
            Assert.AreEqual(origin, actual, "should be equal");
        }

        [Test]
        public void Test_Upstream_FromLocalIJ_BaseCells_OffsetIndex() {
            // Arrange
            H3Index origin = 0x8029fffffffffff;
            CoordIJ offset = (1, 0);
            H3Index expectedIndex = 0x8051fffffffffff;

            // Act
            var actual = origin.FromLocalIJ(offset);

            // Assert
            Assert.AreEqual(expectedIndex, actual, "should be equal");
        }

        [Test]
        [TestCase(2, 0)]
        [TestCase(0, 2)]
        [TestCase(-2, -2)]
        public void Test_Upstream_FromLocalIJ_BaseCells_OutOfRange(int i, int j) {
            // Arrange
            H3Index origin = 0x8029fffffffffff;
            CoordIJ coord = (i, j);

            // Act
            var actual = origin.FromLocalIJ(coord);

            // Assert
            Assert.AreEqual(H3Index.Invalid, actual, "should equal H3_NULL");
        }

        [Test]
        [TestCase(0, 0, 0x81283ffffffffffUL)]
        [TestCase(1, 0, 0x81293ffffffffffUL)]
        [TestCase(2, 0, 0x8150bffffffffffUL)]
        [TestCase(3, 0, 0x8151bffffffffffUL)]
        [TestCase(4, 0, 0UL)]
        [TestCase(-4, 0, 0UL)]
        [TestCase(0, 4, 0UL)]
        public void Test_Upstream_FromLocalIJ(int i, int j, ulong expectedIndex) {
            // Arrange
            H3Index origin = 0x81283ffffffffff;
            CoordIJ coord = (i, j);
            H3Index expected = expectedIndex;

            // Act
            var actual = origin.FromLocalIJ(coord);

            // Assert
            Assert.AreEqual(expected, actual, "should be equal");
        }

    }
}
