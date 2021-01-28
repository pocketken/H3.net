using System;
using System.Linq;
using H3.Algorithms;
using H3.Model;
using NetTopologySuite.Geometries;
using NUnit.Framework;

namespace H3.Test {

    [TestFixture]
    public class H3IndexTests {
        // result of encoding Point(-110, 30) @ Res14 in PG
        public const ulong TestIndexValue = 0x8e48e1d7038d527;

        // result of select h3_to_children('8e48e1d7038d527'::h3index, 15) in PG
        public static readonly ulong[] TestIndexChildrenAtRes15 = new ulong[7] {
            0x8f48e1d7038d520,
            0x8f48e1d7038d521,
            0x8f48e1d7038d522,
            0x8f48e1d7038d523,
            0x8f48e1d7038d524,
            0x8f48e1d7038d525,
            0x8f48e1d7038d526,
        };

        // Cell index values for resolutions 1 -> 14 for TestIndexValue
        public static readonly CellIndex[] TestIndexCellIndexPerResolution = new CellIndex[14] {
            CellIndex.JK,
            CellIndex.I,
            CellIndex.K,
            CellIndex.IJ,
            CellIndex.IK,
            CellIndex.IJ,
            CellIndex.Center,
            CellIndex.K,
            CellIndex.IJ,
            CellIndex.K,
            CellIndex.IK,
            CellIndex.J,
            CellIndex.I,
            CellIndex.I
        };

        [Test]
        public void Test_KnownIndexValue() {
            // Act
            H3Index h3 = new H3Index(TestIndexValue);

            // Assert
            AssertKnownIndexValue(h3);
        }

        [Test]
        public void Test_KnownIndexValue_Children() {
            // Arrange
            H3Index h3 = new H3Index(TestIndexValue);

            // Act
            H3Index[] children = h3.GetChildrenAtResolution(15).ToArray();

            // Assert
            AssertChildren(TestIndexChildrenAtRes15, children);
        }

        [Test]
        public void Test_FromPoint_MatchesKnownIndexValue() {
            // Arrange
            Point point = new Point(-110, 30);

            // Act
            H3Index h3 = H3Index.FromPoint(point, 14);

            // Assert
            AssertKnownIndexValue(h3);
        }

        private static void AssertKnownIndexValue(H3Index h3) {
            Assert.IsTrue(TestIndexValue == h3, "ulong value should equal H3Index");
            Assert.IsTrue(h3.IsValid, "should be valid");
            Assert.IsFalse(h3.IsPentagon, "should not be a pentagon");
            Assert.AreEqual(Mode.Hexagon, h3.Mode, "should be mode of hexagon");
            Assert.AreEqual(14, h3.Resolution, "should be res 14");
            Assert.AreEqual(36, h3.BaseCellNumber, "should be basecell 36");
            Assert.AreEqual(0, h3.ReservedBits, "should have reserved bits of 0");
            Assert.AreEqual(0, h3.HighBit, "should have high bit of 0");

            for (int r = 1; r <= 14; r += 1) {
                Assert.AreEqual(
                    TestIndexCellIndexPerResolution[r-1],
                    h3.GetCellIndexForResolution(r),
                    $"res {r} should have cell index {TestIndexCellIndexPerResolution[r-1]}"
                );
            }
        }

        private static void AssertChildren(ulong[] expectedChildren, H3Index[] actualChildren) {
            Assert.AreEqual(expectedChildren.Length, actualChildren.Length, "should have same length");
            for (int i = 0; i < expectedChildren.Length; i += 1) {
                Assert.IsTrue(expectedChildren[i] == actualChildren[i], "should be same child");
            }
        }

    }

}