using System;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using NetTopologySuite.Geometries;
using NUnit.Framework;

namespace H3.Test.Algorithms {

    [TestFixture]
    public class LineTests {

        // result of select h3_line(h3_geo_to_h3(point(-110, 30), 14), h3_geo_to_h3(point(-110, 30.0005), 14));
        private static readonly H3Index[] TestLineIndicies = new H3Index[] {
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
        public void Test_LineTo_ReturnsExpectedIndicies() {
            // Arrange
            H3Index start = H3Index.FromPoint(new Point(-110, 30), 14);
            H3Index end = H3Index.FromPoint(new Point(-110, 30.0005), 14);

            // Act
            var line = start.LineTo(end).ToArray();

            // Assert
            TestHelpers.AssertAll(TestLineIndicies, line);
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

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 5)]
        public void Test_Upstream_LineTo_KRing_Assertions(int resolution, int k) {
            // Arrange
            var endpoints = TestHelpers.GetAllCellsForResolution(resolution)
                .Where(index => !index.IsPentagon)
                .SelectMany(start =>
                    start
                        .GetKRing(k)
                        .Where(n => n.Index != H3Index.Invalid)
                        .Select(n => (Start: start, End: n.Index, Distance: start.DistanceTo(n.Index)))
                );

            // Act
            var lines = endpoints.Select(e => (e.Start, e.End, e.Distance, Line: e.Start.LineTo(e.End).ToList()));

            // Assert
            foreach (var (Start, End, Distance, Line) in lines) {
                if (Distance >= 0) {
                    Assert.AreEqual(Distance + 1, Line.Count, $"line should have count of {Distance + 1}");
                    Assert.AreEqual(Start, Line.First(), $"line should start with {Start}");
                    Assert.AreEqual(End, Line.Last(), $"line should end with {End}");
                    for (int i = 1; i < Line.Count; i += 1) {
                        var index = Line[i];
                        Assert.IsTrue(index.IsValid, $"{index} should be valid");
                        Assert.IsTrue(index.IsNeighbour(Line[i - 1]), $"{index} should be neighbours with previous index {Line[i-1]}");
                        if (i > 1) {
                            Assert.IsFalse(index.IsNeighbour(Line[i - 2]), $"{index} should not be neighbours with index before previous {Line[i-2]}");
                        }
                    }
                } else {
                    Assert.IsEmpty(Line, "should be empty for invalid distances");
                }
            }
        }
    }
}
