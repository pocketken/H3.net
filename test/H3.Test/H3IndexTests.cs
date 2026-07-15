using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NetTopologySuite.Geometries;
using NUnit.Framework;

using System.Reflection;
using System.Text.RegularExpressions;
#if NET8_0_OR_GREATER
using System.Text.Json;
#endif

namespace H3.Test;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class H3IndexTests {

    /// <summary>
    /// All of the upstream Index -> LatLng tests
    /// </summary>
    public static IEnumerable<TestCaseData> ToGeoCoordTestCases {
        get {
            var testFiles = TestHelpers
                .GetTestData(f => f.Contains("bc") && f.Contains("centers") ||
                                  f.Contains("res") && f.Contains("ic"));

            var executingAssembly = Assembly.GetExecutingAssembly();

            return testFiles.Select(testFile => {
                using var stream = executingAssembly.GetManifestResourceStream(testFile);
                using var reader = new StreamReader(stream);
                return new TestCaseData(TestHelpers.ReadLines(reader)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => {
                        var segs = s.Split(' ');
                        return (
                            new H3Index(segs[0]),
                            Convert.ToDouble(segs[1]),
                            Convert.ToDouble(segs[2])
                        );
                    }).ToArray()).Returns(true);
            });
        }
    }

    /// <summary>
    /// All of the upstream LatLng -> Index tests
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
                        var segs = s.Split(' ');
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
        H3Index h3 = new(TestHelpers.TestIndexValue);

        // Assert
        AssertKnownIndexValue(h3);
    }

    [Test]
    public void Test_FromString_MatchesKnownIndexValue() {
        // Act
        H3Index h3 = new("8e48e1d7038d527");

        // Assert
        AssertKnownIndexValue(h3);
    }

    [Test]
    public void Test_FromPoint_MatchesKnownIndexValue() {
        // Arrange
        Point point = new(-110, 30);

        // Act
        var h3 = H3Index.FromPoint(point, 14);

        // Assert
        AssertKnownIndexValue(h3);
    }

    [Test]
    public void Test_Equality() {
        // Arrange
        H3Index i1 = new(TestHelpers.TestIndexValue);
        H3Index i1_1 = new(TestHelpers.TestIndexValue);
        H3Index i2 = new(TestHelpers.TestIndexValue + 1);
        H3Index i2_2 = new(TestHelpers.TestIndexValue + 1);
        List<H3Index> h3List = new() { i1, i2 };
        HashSet<H3Index> h3Set = new() { i1, i2 };

        // Assert
        Assert.That(h3List.Exists(e => e == i1), Is.True, "should exist");
        Assert.That(h3List.Exists(e => e == i1_1), Is.True, "should exist"); // same value as i1
        Assert.That(h3List.Exists(e => e == TestHelpers.TestIndexValue), Is.True, "should exist");
        Assert.That(h3List.Exists(e => e == TestHelpers.TestIndexValue + 1), Is.True, "should exist");
        Assert.That(h3List.Exists(e => e == 0UL), Is.False, "should not exist");
        Assert.That(h3Set.Contains(i1_1), Is.True, "should contain i1_1");
        Assert.That(h3Set.Contains(i2_2), Is.True, "should contain i2_2");
        Assert.That(h3Set.Contains(TestHelpers.TestIndexValue), Is.True, "should contain TestIndexValue");
        Assert.That(h3Set.Contains(0), Is.False, "should not contain 0");
    }

    [Test]
    [TestCaseSource(typeof(H3IndexTests), "ToGeoCoordTestCases")]
    public bool Test_Upstream_ToGeoCoord((H3Index, double, double)[] expectedValues) {
        // Act
        var actualCoords = expectedValues.Select(t => t.Item1.ToLatLng()).ToArray();

        // Assert — expected values are authoritative libh3 degrees; hold ours to MaxUlps.
        for (var i = 0; i < expectedValues.Length; i += 1) {
            var (_, expectedLatitude, expectedLongitude) = expectedValues[i];
            var actualCoord = actualCoords[i];
            var matches = TestHelpers.CheckUlps("ToLatLng.latDeg", actualCoord.LatitudeDegrees, expectedLatitude)
                        & TestHelpers.CheckUlps("ToLatLng.lngDeg", actualCoord.LongitudeDegrees, expectedLongitude);
            if (!matches) {
                Assert.Fail($"expected: {expectedLatitude},{expectedLongitude} actual: {actualCoord.LatitudeDegrees},{actualCoord.LongitudeDegrees} ulp: {TestHelpers.UlpDistance(actualCoord.LatitudeDegrees, expectedLatitude)},{TestHelpers.UlpDistance(actualCoord.LongitudeDegrees, expectedLongitude)}");
                return false;
            }
        }

        return true;
    }

    [Test]
    [TestCaseSource(typeof(H3IndexTests), "FromGeoCoordTestCases")]
    public bool Test_Upstream_FromGeoCoord((double, double, int, H3Index)[] expectedValues) {
        // Act
        var actualIndexes = expectedValues.Select(t => H3Index.FromLatLng((t.Item1, t.Item2), t.Item3)).ToArray();

        // Assert
        for (var i = 0; i < expectedValues.Length; i += 1) {
            var expectedIndex = expectedValues[i].Item4;
            var actualIndex = actualIndexes[i];
            if (expectedIndex != actualIndex) {
                return false;
            }
        }

        return true;
    }

    [Test]
    public void Test_Upstream_IsValid_InvalidBaseCell() {
        // Arrange
        var index = new H3Index {
            BaseCellNumber = 122
        };

        // Act
        var actual = index.IsValidCell;

        // Assert
        Assert.That(actual, Is.False, "should not be valid (invalid base cell)");
    }

    [Test]
    [TestCase("0")]
    [TestCase("2")]
    [TestCase("3")]
    [TestCase("4")]
    [TestCase("5")]
    [TestCase("6")]
    [TestCase("7")]
    [TestCase("8")]
    [TestCase("9")]
    [TestCase("10")]
    [TestCase("11")]
    [TestCase("12")]
    [TestCase("13")]
    [TestCase("14")]
    [TestCase("15")]
    public void Test_Upstream_IsValid_InvalidMode(string modeValue) {
        // Arrange
#if NET48
        var mode = (Mode)Enum.Parse(typeof(Mode), modeValue, true);
#else
            var mode = Enum.Parse<Mode>(modeValue);
#endif
        var index = new H3Index {
            Mode = mode
        };

        // Act
        var actual = index.IsValidCell;

        // Assert
        Assert.That(actual, Is.False, "should not be valid (invalid mode)");
    }

    [Test]
    public void Test_Upstream_IsValid_InvalidHighBit() {
        // Arrange
        var index = new H3Index {
            HighBit = 1
        };

        // Act
        var actual = index.IsValidCell;

        // Assert
        Assert.That(actual, Is.False, "should not be valid (invalid high bit)");
    }

    [Test]
    public void Test_Upstream_IsValid_InvalidDigit() {
        // Arrange
        var index = new H3Index {
            Resolution = 1
        };

        // Act
        var actual = index.IsValidCell;

        // Assert
        Assert.That(actual, Is.False, "should not be valid (invalid/too large digit)");
    }

    [Test]
    public void Test_Upstream_IsValid_InvalidDeletedSubsequence() {
        // Arrange
        var index = H3Index.Create(1, 4, Direction.K);

        // Act
        var actual = index.IsValidCell;

        // Assert
        Assert.That(actual, Is.False, "should not be valid (deleted subsequence)");
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    public void Test_GH111_IsValidCell_TrueForAllResolutions(int resolution) {
        // Arrange
        var point = new Point(24.57011413572222, 60.191627502416665) { SRID = 4326 };

        // Act
        var index = H3Index.FromPoint(point, resolution);

        // Assert
        Assert.That(index.IsValidCell, Is.True, $"{index} should be valid at resolution {resolution}");
    }

    [Test]
    public void Test_GH111_IsValidCell_RejectsInvalidDigitAtHighResolution() {
        // Arrange
        var index = new H3Index(TestHelpers.SfIndex) {
            Resolution = 15
        };
        index.ZeroDirectionsForResolutionRange(10, 15);
        index.SetDirectionForResolution(12, Direction.Invalid);

        // Act
        var actual = index.IsValidCell;

        // Assert
        Assert.That(actual, Is.False, "should not be valid (invalid digit at resolution 12)");
    }

    [Test]
    public void Test_GH111_IsValidCell_RejectsUnusedDigitAtLowResolution() {
        // Arrange
        var index = new H3Index {
            Mode = Mode.Cell
        };
        index.SetDirectionForResolution(15, Direction.Center);

        // Act
        var actual = index.IsValidCell;

        // Assert
        Assert.That(actual, Is.False, "should not be valid (unused digit must be Invalid)");
    }

    [Test]
    [TestCase(0, ExpectedResult = 122L)]
    [TestCase(1, ExpectedResult = 842L)]
    [TestCase(15, ExpectedResult = 569707381193162L)]
    public long Test_Upstream_GetNumberOfCells(int resolution) {
        return H3Index.GetNumberOfCells(resolution);
    }

    [Test]
    [TestCase(-1)]
    [TestCase(16)]
    public void Test_Upstream_GetNumberOfCells_InvalidResolution(int resolution) {
        // Act
        Action actual = () => H3Index.GetNumberOfCells(resolution);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(actual, "should throw for out of range resolution");
    }

    [Test]
    public void Test_Upstream_GetRes0Cells() {
        // Act
        var cells = H3Index.GetRes0Cells().ToArray();

        // Assert
        Assert.That(cells.Length, Is.EqualTo(122), "should return 122 cells");
        Assert.That(cells.All(cell => cell.IsValidCell), Is.True, "all cells should be valid");
        Assert.That(cells.Distinct().Count(), Is.EqualTo(122), "cells should be unique");
    }

    [Test]
    public void Test_Upstream_GetPentagons([Range(0, MAX_H3_RES)] int resolution) {
        // Act
        var pentagons = H3Index.GetPentagons(resolution);

        // Assert
        Assert.That(pentagons.Count, Is.EqualTo(NUM_PENTAGONS), $"should return {NUM_PENTAGONS} pentagons at res {resolution}");
        foreach (var pentagon in pentagons) {
            Assert.That(pentagon.IsValidCell, Is.True, $"{pentagon} should be valid");
            Assert.That(pentagon.IsPentagon, Is.True, $"{pentagon} should be a pentagon");
            Assert.That(pentagon.Resolution, Is.EqualTo(resolution), "should be at the requested resolution");
        }
    }

    [Test]
    [TestCase(0, 4357449.4160783831)]
    [TestCase(15, 8.9531159076057898e-07)]
    public void Test_Upstream_GetHexagonAreaAverageInKmSquared(int resolution, double expected) {
        // Act
        var actual = H3Index.GetHexagonAreaAverageInKmSquared(resolution);

        // Assert
        Assert.That(TestHelpers.CheckUlps("HexTable", actual, expected), Is.True, $"should match upstream table (ulp {TestHelpers.UlpDistance(actual, expected)})");
    }

    [Test]
    [TestCase(0, 4357449416078.3901)]
    [TestCase(15, 0.8953115907605802)]
    public void Test_Upstream_GetHexagonAreaAverageInMSquared(int resolution, double expected) {
        // Act
        var actual = H3Index.GetHexagonAreaAverageInMSquared(resolution);

        // Assert
        Assert.That(TestHelpers.CheckUlps("HexTable", actual, expected), Is.True, $"should match upstream table (ulp {TestHelpers.UlpDistance(actual, expected)})");
    }

    [Test]
    [TestCase(0, 1281.2560109999999)]
    [TestCase(15, 0.00058416900000000005)]
    public void Test_Upstream_GetHexagonEdgeLengthAverageInKm(int resolution, double expected) {
        // Act
        var actual = H3Index.GetHexagonEdgeLengthAverageInKm(resolution);

        // Assert
        Assert.That(TestHelpers.CheckUlps("HexTable", actual, expected), Is.True, $"should match upstream table (ulp {TestHelpers.UlpDistance(actual, expected)})");
    }

    [Test]
    [TestCase(0, 1281256.0109999999)]
    [TestCase(15, 0.58416862999999997)]
    public void Test_Upstream_GetHexagonEdgeLengthAverageInM(int resolution, double expected) {
        // Act
        var actual = H3Index.GetHexagonEdgeLengthAverageInM(resolution);

        // Assert
        Assert.That(TestHelpers.CheckUlps("HexTable", actual, expected), Is.True, $"should match upstream table (ulp {TestHelpers.UlpDistance(actual, expected)})");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(16)]
    public void Test_Upstream_GetHexagonEdgeLengthAverageInM_InvalidResolution(int resolution) {
        // Act
        Action actual = () => H3Index.GetHexagonEdgeLengthAverageInM(resolution);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(actual, "should throw for out of range resolution");
    }

    [Test]
    public void Test_Upstream_ConstructCell_RoundTripsDigits() {
        // Arrange
        var expected = new H3Index(TestHelpers.SfIndex);
        var resolution = expected.Resolution;
        var digits = Enumerable.Range(1, resolution)
            .Select(r => expected.GetDirectionForResolution(r))
            .ToArray();

        // Act
        var actual = H3Index.Create(resolution, expected.BaseCellNumber, digits);

        // Assert
        Assert.That(actual, Is.EqualTo(expected), "should reconstruct the index from its components");
    }

    [Test]
    public void Test_Upstream_ConstructCell_InvalidComponents() {
        // Arrange
        var digits = new[] { Direction.K, Direction.J };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => H3Index.Create(-1, 0, digits), "should throw for negative resolution");
        Assert.Throws<ArgumentOutOfRangeException>(() => H3Index.Create(2, 122, digits), "should throw for invalid base cell");
        Assert.Throws<ArgumentException>(() => H3Index.Create(3, 0, digits), "should throw for insufficient digits");
        Assert.Throws<ArgumentOutOfRangeException>(() => H3Index.Create(1, 0, new[] { Direction.Invalid }), "should throw for out of range digit");
        Assert.Throws<ArgumentException>(() => H3Index.Create(1, 4, new[] { Direction.K }), "should throw for deleted pentagon subsequence");
    }

    [Test]
    public void Test_IsValidIndex_TrueForCell() {
        // Arrange
        var cell = new H3Index(TestHelpers.SfIndex);

        // Act
        var actual = cell.IsValidIndex;

        // Assert
        Assert.That(actual, Is.True, "cell should be a valid index");
    }

    [Test]
    public void Test_IsValidIndex_TrueForDirectedEdge() {
        // Arrange
        var cell = new H3Index(TestHelpers.SfIndex);
        var edge = cell.ToDirectedEdge(cell.GetDirectNeighbour(Direction.I).Item1);

        // Act
        var actual = edge.IsValidIndex;

        // Assert
        Assert.That(actual, Is.True, "directed edge should be a valid index");
    }

    [Test]
    public void Test_IsValidIndex_TrueForVertex() {
        // Arrange
        var vertex = new H3Index(TestHelpers.SfIndex).CellToVertex(0);

        // Act
        var actual = vertex.IsValidIndex;

        // Assert
        Assert.That(actual, Is.True, "vertex should be a valid index");
    }

    [Test]
    [TestCase(0UL)]
    [TestCase(ulong.MaxValue)]
    public void Test_IsValidIndex_FalseForNonIndex(ulong value) {
        // Arrange
        var index = new H3Index(value);

        // Act
        var actual = index.IsValidIndex;

        // Assert
        Assert.That(actual, Is.False, "should not be a valid index");
    }

#if NET8_0_OR_GREATER
    [Test]
    public void Test_Serialization_ToJson() {
        // Arrange
        var expected = $@"""{TestHelpers.SfIndex}""";


        // Act
        var result = JsonSerializer.Serialize(TestHelpers.SfIndex);

        // Assert
        Assert.That(result, Is.Not.Null, "should not be null");
        Assert.That(result, Is.EqualTo(expected), "should serialize to hex string");
    }

    [Test]
    public void Test_Serialization_FromJson() {
        // Arrange
        var indexJson = $@"""{TestHelpers.SfIndex}""";

        // Act
        var result = JsonSerializer.Deserialize<H3Index>(indexJson);

        // Assert
        Assert.That(result, Is.EqualTo(TestHelpers.SfIndex), "should be equal");
    }

    [Test]
    public void Test_Serialization_FromJson_CaseInsensitive() {
        // Arrange
        var indexJson = $@"""{TestHelpers.SfIndex.ToString().ToUpperInvariant()}""";

        // Act
        var result = JsonSerializer.Deserialize<H3Index>(indexJson);

        // Assert
        Assert.That(result, Is.EqualTo(TestHelpers.SfIndex), "should be equal");
    }

    [Test]
    public void Test_Serialization_FromJson_ShouldNotSwallowInvalidStringValues() {
        // Arrange
        var indexJson = @"""zonk""";

        // Act
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<H3Index>(indexJson));

        // Assert
        Assert.That(exception, Is.Not.Null, "should not be null");
        Assert.That(exception.Message, Is.EqualTo("Not a valid H3 hex string"), "should be equal");
    }

    [Test]
    public void Test_Serialization_FromJson_ShouldNotSwallowEmptyStringValues() {
        // Arrange
        var indexJson = @"""""";

        // Act
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<H3Index>(indexJson));

        // Assert
        Assert.That(exception, Is.Not.Null, "should not be null");
        Assert.That(exception.Message, Is.EqualTo("Not a valid H3 hex string"), "should be equal");
    }

    internal record SerializationTest {
        public int SomeOtherProperty { get; set; }
        public H3Index Index { get; set; }
    }

    [Test]
    public void Test_Serialization_ToFromJsonObject() {
        // Arrange
        var record = new SerializationTest { Index = TestHelpers.SfIndex, SomeOtherProperty = 242 };
        var indexJson = JsonSerializer.Serialize(record);

        // Act
        var result = JsonSerializer.Deserialize<SerializationTest>(indexJson);

        // Assert
        Assert.That(result.SomeOtherProperty, Is.EqualTo(242), "should have sentinel value");
        Assert.That(result.Index, Is.EqualTo(TestHelpers.SfIndex), "should be equal");
    }
#endif


    private static void AssertKnownIndexValue(H3Index h3) {
        Assert.That(TestHelpers.TestIndexValue == h3, Is.True, "ulong value should equal H3Index");
        Assert.That(h3.IsValidCell, Is.True, "should be valid");
        Assert.That(h3.IsPentagon, Is.False, "should not be a pentagon");
        Assert.That(h3.Mode, Is.EqualTo(Mode.Cell), "should be mode of hexagon");
        Assert.That(h3.Resolution, Is.EqualTo(14), "should be res 14");
        Assert.That(h3.BaseCellNumber, Is.EqualTo(36), "should be basecell 36");
        Assert.That(h3.ReservedBits, Is.EqualTo(0), "should have reserved bits of 0");
        Assert.That(h3.HighBit, Is.EqualTo(0), "should have high bit of 0");

        for (var r = 1; r <= 14; r += 1) {
            Assert.That(h3.GetDirectionForResolution(r), Is.EqualTo(TestHelpers.TestIndexDirectionPerResolution[r-1]), $"res {r} should have cell index {TestHelpers.TestIndexDirectionPerResolution[r-1]}");
        }
    }

}