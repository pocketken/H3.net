using System;
using System.Linq;
using H3;
using H3.Algorithms;
using NUnit.Framework;

namespace H3.Test.Algorithms {

    [TestFixture]
    public class RingTests {

        [Test]
        public void Test_GetHexDistances_KnownValue() {
            // Act
            var (hexDistanceResult, hexDistanceList) = new H3Index(TestHelpers.TestIndexValue).GetHexRangeDistances(2);

            // Assert
            Assert.AreEqual(HexRingResult.Success, hexDistanceResult, "should be successful");

            var hexDistances = hexDistanceList.ToArray();
            Assert.AreEqual(TestHelpers.TestIndexKRingsTo2.Length, hexDistances.Length, "should be same length");
            for (int i = 0; i < TestHelpers.TestIndexKRingsTo2.Length; i += 1) {
                var expected = TestHelpers.TestIndexKRingsTo2[i];
                var actual = hexDistances[i];

                Assert.IsTrue(expected.Item1 == actual.Index, $"should be same index {expected.Item1}");
                Assert.AreEqual(expected.Item2, actual.Distance, $"should be same distance {expected.Item2}");
            }
        }

        [Test]
        public void Test_GetKRingDistances_KnownValue() {
            // Act
            var ringDistanceList = new H3Index(TestHelpers.TestIndexValue).GetKRingDistances(2);

            // Assert
            Assert.IsNotEmpty(ringDistanceList, "should not be empty");
        }

    }
}
