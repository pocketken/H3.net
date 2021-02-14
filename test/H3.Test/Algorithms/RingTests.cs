using System;
using System.Linq;
using H3.Algorithms;
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

        private static void AssertRing((ulong, int)[] expectedRing, RingCell[] actualRing) {
            Assert.AreEqual(expectedRing.Length, actualRing.Length, "should be same length");
            for (int i = 0; i < expectedRing.Length; i += 1) {
                var expected = expectedRing[i];
                var actual = actualRing[i];

                Assert.IsNotNull(actualRing.Where(cell => cell.Index == expected.Item1 && cell.Distance == expected.Item2).First(), $"can't find {expected.Item1:x} at k {expected.Item2}");
            }
        }

    }
}
