using System;
using System.Collections.Generic;
using System.Linq;
using H3.Model;
using NetTopologySuite.Geometries;
using NUnit.Framework;

namespace H3.Test {

    [TestFixture]
    public class H3IndexTests {

        [Test]
        public void Test_KnownIndexValue() {
            // Act
            H3Index h3 = new H3Index(TestHelpers.TestIndexValue);

            // Assert
            AssertKnownIndexValue(h3);
        }

        [Test]
        public void Test_KnownIndexValue_Children() {
            // Arrange
            H3Index h3 = new H3Index(TestHelpers.TestIndexValue);

            // Act
            H3Index[] children = h3.GetChildrenAtResolution(15).ToArray();

            // Assert
            AssertChildren(TestHelpers.TestIndexChildrenAtRes15, children);
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

        [Test]
        public void Test_Equality() {
            // Arrange
            H3Index i1 = new H3Index(TestHelpers.TestIndexValue);
            H3Index i1_1 = new H3Index(TestHelpers.TestIndexValue);
            H3Index i2 = new H3Index(TestHelpers.TestIndexValue + 1);
            H3Index i2_2 = new H3Index(TestHelpers.TestIndexValue + 1);
            List<H3Index> h3List = new() { i1, i2 };
            HashSet<H3Index> h3Set = new() { i1, i2 };

            // Assert
            Assert.IsTrue(h3List.Exists(e => e == TestHelpers.TestIndexValue), "should exist");
            Assert.IsTrue(h3List.Exists(e => e == TestHelpers.TestIndexValue + 1), "should exist");
            Assert.IsFalse(h3List.Exists(e => e == 0UL), "should not exist");
            Assert.IsTrue(h3Set.Contains(i1_1), "should contain i1_1");
            Assert.IsTrue(h3Set.Contains(i2_2), "should contain i2_2");
            Assert.IsTrue(h3Set.Contains(TestHelpers.TestIndexValue), "should contain TestIndexValue");
            Assert.IsFalse(h3Set.Contains(0), "should not contain 0");
        }

        private static void AssertKnownIndexValue(H3Index h3) {
            Assert.IsTrue(TestHelpers.TestIndexValue == h3, "ulong value should equal H3Index");
            Assert.IsTrue(h3.IsValid, "should be valid");
            Assert.IsFalse(h3.IsPentagon, "should not be a pentagon");
            Assert.AreEqual(Mode.Hexagon, h3.Mode, "should be mode of hexagon");
            Assert.AreEqual(14, h3.Resolution, "should be res 14");
            Assert.AreEqual(36, h3.BaseCellNumber, "should be basecell 36");
            Assert.AreEqual(0, h3.ReservedBits, "should have reserved bits of 0");
            Assert.AreEqual(0, h3.HighBit, "should have high bit of 0");

            for (int r = 1; r <= 14; r += 1) {
                Assert.AreEqual(
                    TestHelpers.TestIndexCellIndexPerResolution[r-1],
                    h3.GetCellIndexForResolution(r),
                    $"res {r} should have cell index {TestHelpers.TestIndexCellIndexPerResolution[r-1]}"
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