using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using H3.Model;
using static H3.Constants;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using System.Reflection;
using System.Text.RegularExpressions;

namespace H3.Test {

    [TestFixture]
    public class H3IndexTests {

        /// <summary>
        /// All of the upstream Index -> GeoCoord tests
        /// </summary>
        public static IEnumerable<TestCaseData> ToGeoCoordTestCases {
            get {
                var testFiles = TestHelpers
                    .GetTestData(f => (f.Contains("bc") && f.Contains("centers")) ||
                        (f.Contains("res") && f.Contains("ic")));

                var executingAssembly = Assembly.GetExecutingAssembly();

                return testFiles.Select(testFile => {
                    using var stream = executingAssembly.GetManifestResourceStream(testFile);
                    using var reader = new StreamReader(stream);
                    return new TestCaseData(TestHelpers.ReadLines(reader)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => {
                            var segs = s.Split(" ");
                            return (
                                new H3Index(segs[0]),
                                Convert.ToDouble(segs[1]) * M_PI_180,
                                Convert.ToDouble(segs[2]) * M_PI_180
                            );
                        }).ToArray()).Returns(true);
                });
            }
        }

        /// <summary>
        /// All of the upstream GeoCoord -> Index tests
        /// </summary>
        public static IEnumerable<TestCaseData> FromGeoCoordTestCases {
            get {
                var testFiles = TestHelpers
                    .GetTestData(f => f.Contains("rand") && f.Contains("centers"));

                var executingAssembly = Assembly.GetExecutingAssembly();

                return testFiles.Select(testFile => {
                    var matches = Regex.Match(testFile, @"rand([0-9]+)centers");
                    using var stream = executingAssembly.GetManifestResourceStream(testFile);
                    using var reader = new StreamReader(stream);
                    return new TestCaseData(TestHelpers.ReadLines(reader)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => {
                            var segs = s.Split(" ");
                            return (
                                Convert.ToDouble(segs[1]) * M_PI_180,
                                Convert.ToDouble(segs[2]) * M_PI_180,
                                Convert.ToInt32(matches.Groups[1].Value),
                                new H3Index(segs[0])
                            );
                        }).ToArray()).Returns(true);
                });

            }
        }

        [Test]
        public void Test_KnownIndexValue() {
            // Act
            H3Index h3 = new H3Index(TestHelpers.TestIndexValue);

            // Assert
            AssertKnownIndexValue(h3);
        }

        [Test]
        public void Test_FromString_MatchesKnownIndexValue() {
            // Act
            H3Index h3 = new H3Index("8e48e1d7038d527");

            // Assert
            AssertKnownIndexValue(h3);
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
            Assert.IsTrue(h3List.Exists(e => e == i1), "should exist");
            Assert.IsTrue(h3List.Exists(e => e == i1_1), "should exist"); // same value as i1
            Assert.IsTrue(h3List.Exists(e => e == TestHelpers.TestIndexValue), "should exist");
            Assert.IsTrue(h3List.Exists(e => e == TestHelpers.TestIndexValue + 1), "should exist");
            Assert.IsFalse(h3List.Exists(e => e == 0UL), "should not exist");
            Assert.IsTrue(h3Set.Contains(i1_1), "should contain i1_1");
            Assert.IsTrue(h3Set.Contains(i2_2), "should contain i2_2");
            Assert.IsTrue(h3Set.Contains(TestHelpers.TestIndexValue), "should contain TestIndexValue");
            Assert.IsFalse(h3Set.Contains(0), "should not contain 0");
        }

        [Test]
        [TestCaseSource(typeof(H3IndexTests), "ToGeoCoordTestCases")]
        public bool Test_Upstream_ToGeoCoord((H3Index, double, double)[] expectedValues) {
            // Act
            GeoCoord[] actualCoords = expectedValues.Select(t => t.Item1.ToGeoCoord()).ToArray();

            // Assert
            for (int i = 0; i < expectedValues.Length; i += 1) {
                var (_, expectedLatitude, expectedLongitude) = expectedValues[i];
                var actualCoord = actualCoords[i];
                var matches = Math.Abs(expectedLatitude - actualCoord.Latitude) < 0.000001 &&
                    Math.Abs(expectedLongitude - actualCoord.Longitude) < 0.000001;
                if (!matches) {
                    return false;
                }
            }

            return true;
        }

        [Test]
        [TestCaseSource(typeof(H3IndexTests), "FromGeoCoordTestCases")]
        public bool Test_Upstream_FromGeoCoord((double, double, int, H3Index)[] expectedValues) {
            // Act
            H3Index[] actualIndexes = expectedValues.Select(t => H3Index.FromGeoCoord((t.Item1, t.Item2), t.Item3)).ToArray();

            // Assert
            for (int i = 0; i < expectedValues.Length; i += 1) {
                var expectedIndex = expectedValues[i].Item4;
                var actualIndex = actualIndexes[i];
                if (expectedIndex != actualIndex) {
                    return false;
                }
            }

            return true;
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
                    TestHelpers.TestIndexDirectionPerResolution[r-1],
                    h3.GetDirectionForResolution(r),
                    $"res {r} should have cell index {TestHelpers.TestIndexDirectionPerResolution[r-1]}"
                );
            }
        }

    }

}