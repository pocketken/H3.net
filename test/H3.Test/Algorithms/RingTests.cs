using System;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using NUnit.Framework;

namespace H3.Test.Algorithms {

    [TestFixture]
    public class RingTests {

        [Test]
        public void Test_GetKRingSlow_KnownValue() {
            // Act
            var actual = new H3Index(TestHelpers.TestIndexValue).GetKRingSlow(2);

            // Assert
            AssertRing(TestHelpers.TestIndexKRingsTo2, actual.ToArray());
        }

        [Test]
        public void Test_GetKRingFast_KnownValue() {
            // Act
            var ringDistanceList = new H3Index(TestHelpers.TestIndexValue).GetKRingFast(2);

            // Assert
            AssertRing(TestHelpers.TestIndexKRingsTo2, ringDistanceList.ToArray());
        }

        [Test]
        public void Test_Upstream_GetKRing() {
            // Arrange
            var index = H3Index.FromGeoCoord((0.659966917655, 2 * 3.14159 - 2.1364398519396), 0);
            (ulong, int)[] expected = {
                (0x8029fffffffffff, 0),
                (0x801dfffffffffff, 1),
                (0x8013fffffffffff, 1),
                (0x8027fffffffffff, 1),
                (0x8049fffffffffff, 1),
                (0x8051fffffffffff, 1),
                (0x8037fffffffffff, 1)
            };

            // Act
            var ring = index.GetKRing(1).ToArray();

            // Assert
            AssertRing(expected, ring);
        }

        [Test]
        public void Test_Upstream_GetKRing_PolarPentagonRes0() {
            // Arrange
            var index = TestHelpers.CreateIndex(0, 4, 0);
            (ulong, int)[] expected = {
                (0x8009fffffffffff, 0),
                (0x8007fffffffffff, 1),
                (0x8001fffffffffff, 1),
                (0x8011fffffffffff, 1),
                (0x801ffffffffffff, 1),
                (0x8019fffffffffff, 1),
                // NOTE upstream has a 0 here in their test?
            };

            // Act
            var ring = index.GetKRing(1).ToArray();

            // Assert
            AssertRing(expected, ring);
        }

        [Test]
        public void Test_Upstream_GetKRing_PolarPentagonRes1() {
            // Arrange
            var index = TestHelpers.CreateIndex(1, 4, 0);
            (ulong, int)[] expected = {
                (0x81083ffffffffff, 0),
                (0x81093ffffffffff, 1),
                (0x81097ffffffffff, 1),
                (0x8108fffffffffff, 1),
                (0x8108bffffffffff, 1),
                (0x8109bffffffffff, 1),
                (0, 1)
                // TODO why do we get 0 here and not up there?  Slow vs Fast?
            };

            // Act
            var ring = index.GetKRing(1).ToArray();

            // Assert
            AssertRing(expected, ring);
        }

        private static void AssertRing((ulong, int)[] expectedRing, RingCell[] actualRing) {
            Assert.AreEqual(expectedRing.Length, actualRing.Length, "should be same length");
            for (int i = 0; i < expectedRing.Length; i += 1) {
                var expected = expectedRing[i];

                Assert.IsNotNull(actualRing.Where(cell => cell.Index == expected.Item1 && cell.Distance == expected.Item2).FirstOrDefault(), $"can't find {expected.Item1:x} at k {expected.Item2}");
            }
        }

    }
}
