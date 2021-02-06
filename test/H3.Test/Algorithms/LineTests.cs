using System;
using System.Linq;
using H3.Algorithms;
using NetTopologySuite.Geometries;
using NUnit.Framework;

namespace H3.Test.Algorithms {

    [TestFixture]
    public class LineTests {

         // result of select h3_line(h3_geo_to_h3(point(-110, 30), 14), h3_geo_to_h3(point(-110, 30.0005), 14));
        public static readonly H3Index[] TestLineIndicies = new H3Index[] {
            0x8e48e1d7038d527,
            0x8e48e1d7038d507,
            0x8e48e1d7038d50f,
            0x8e48e1d7038d427,
            0x8e48e1d7038d407,
            0x8e48e1d7038d40f,
            0x8e48e1d7038d4e7,
            0x8e48e1d7038d4ef,
            0x8e48e1d7038d4cf,
            0x8e48e1d70388b67,
            0x8e48e1d70388b6f,
            0x8e48e1d70388b4f,
            0x8e48e1d70388a67,
            0x8e48e1d70388a6f,
            0x8e48e1d70388a4f,
            0x8e48e1d70389da7,
            0x8e48e1d70389daf,
            0x8e48e1d70389d8f,
            0x8e48e1d70389c17,
            0x8e48e1d70389caf,
            0x8e48e1d70389c8f,
            0x8e48e1d70389cd7,
            0x8e48e1d7038952f
        };

        [Test]
        public void Test_Line_ReturnsExpectedIndicies() {
            // Arrange
            H3Index start = H3Index.FromPoint(new Point(-110, 30), 14);
            H3Index end = H3Index.FromPoint(new Point(-110, 30.0005), 14);

            // Act
            var line = start.LineTo(end).ToArray();

            // Assert
            AssertLinesAreEqual(TestLineIndicies, line);
        }

        [Test]
        public void Test_DistanceTo_FailsAcrossMultipleFaces() {
            // Arrange
            H3Index start = 0x85285aa7fffffff;
            H3Index end = 0x851d9b1bfffffff;

            // Act
            var lineSize = start.DistanceTo(end);

            // Assert
            Assert.AreEqual(-1, lineSize, "line size should be -1");
        }

        private static void AssertLinesAreEqual(H3Index[] expected, H3Index[] actual) {
            Assert.AreEqual(expected.Length, actual.Length, "should have same length");
            for (int i = 0; i < expected.Length; i += 1) {
                Assert.IsTrue((ulong)expected[i] == (ulong)actual[i], $"index {i + 1} should equal {expected[i]}, not {actual[i]}");
            }
        }

    }
}
