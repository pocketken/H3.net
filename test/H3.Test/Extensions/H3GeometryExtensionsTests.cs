using System;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NUnit.Framework;

using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace H3.Test.Extensions;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class H3GeometryExtensionsTests {

    private static readonly H3Index[] PentagonFaceIndicies = {
        H3Index.Create(1, 4, 0),
        H3Index.Create(2, 4, 0),
        H3Index.Create(15, 4, 0)
    };

    // authoritative Uber libh3 v4.5.0 cellAreaKm2(latLngToCell(0,0,r)), r = 0..15,
    // full round-trippable precision (%.17g), built -ffp-contract=off
    private static readonly double[] CellAreasInKm2 = {
        2562182.1629554969, 447684.20172018639, 65961.622427110298,
        9228.8729190026515, 1318.6944907970751, 187.95935122812688,
        26.871643547623222, 3.8408488470593798, 0.54869396413390203,
        0.078386008086490239, 0.011198342220009625, 0.0015997771692882739,
        0.00022853909314890188, 3.2648502336509069e-05, 4.6640703245025627e-06,
        6.6629576002927966e-07
    };

    // authoritative Uber libh3 v4.5.0 cellToBoundary('8075fffffffffff'), Point(X=lng, Y=lat),
    // full round-trippable precision (%.17g), built -ffp-contract=off
    private static readonly Point[] Res0BoundaryVertices = {
        new(-4.0139984434704905, 11.545295975414763),
        new(-13.708146703917997, 6.2709651362757732),
        new(-11.664747542126426, -4.4670316097845237),
        new(-0.78283917510552126, -5.8899217543139173),
        new(3.9430361557864577, 3.9687969766095761),
    };

    // authoritative Uber libh3 v4.5.0 cellToBoundary('8e48e1d7038d527'), Point(X=lng, Y=lat),
    // full round-trippable precision (%.17g), built -ffp-contract=off
    private static readonly Point[] TestPointRes14BoundaryVertices = {
        new(-110.00000042910071, 29.999989232744888),
        new(-109.9999866038296, 29.999998686129619),
        new(-109.99998915205138, 30.000013715927761),
        new(-110.00000552554795, 30.00001929234061),
        new(-110.00001935081971, 30.000009838954508),
        new(-110.00001680259425, 29.999994809156938)
    };

    // select st_astext(h3_to_geo_boundary_geometry('8e48e1d7038d527'::h3index));
    public const string TestPointBoundaryPolygonWkt = "POLYGON ((-110.000000429101 "
                                                      + "29.9999892327449, -109.99998660383 29.9999986861296, -109.999989152051 "
                                                      + "30.0000137159278, -110.000005525548 30.0000192923406, -110.00001935082 "
                                                      + "30.0000098389545, -110.000016802594 29.9999948091569, -110.000000429101 "
                                                      + "29.9999892327449))";

    public static IEnumerable<TestCaseData> GetCellBoundaryVerticesTestCases {
        get {
            var testFiles = TestHelpers
                .GetTestData(f => f.Contains("cells"));

            var executingAssembly = Assembly.GetExecutingAssembly();

            return testFiles.Select(testFile => {
                using var stream = executingAssembly.GetManifestResourceStream(testFile);
                if (stream == null) return null;

                using var reader = new StreamReader(stream);

                // authoritative libh3 boundary vertices, stored as (lat, lng) degrees
                List<(H3Index, (double Lat, double Lng)[])> data = new();
                string line;
                H3Index index = H3Index.Invalid;
                List<(double Lat, double Lng)> coords = null;

                while ((line = reader.ReadLine()) != null) {
                    if (index == H3Index.Invalid) {
                        index = new H3Index(line);
                        continue;
                    }
                    switch (line) {
                        case "{":
                            coords = new List<(double Lat, double Lng)>();
                            continue;
                        case "}":
                            data.Add((index, coords!.ToArray()));
                            index = 0;
                            coords = null;
                            continue;
                    }

                    if (coords == null)
                        continue;

                    var match = Regex.Match(line, @"\s+([0-9.eE+-]+) ([0-9.eE+-]+)");
                    coords.Add((
                        Convert.ToDouble(match.Groups[1].Value),
                        Convert.ToDouble(match.Groups[2].Value))
                    );
                }

                return new TestCaseData(testFile, data).Returns(true);
            });
        }
    }

    [Test]
    public void Test_GetCellBoundaryVertices_AtRes0() {
        // Arrange
        LatLng c = new(0, 0);
        var index = H3Index.FromLatLng(c, 0);

        // Act
        var boundary = index.GetCellBoundaryVertices().ToArray();

        // Assert
        AssertCellBoundaryVertices(Res0BoundaryVertices, boundary);
    }

    [Test]
    public void Test_GetCellBoundaryVertices_KnownValue() {
        // Act
        var boundary = new H3Index(TestHelpers.TestIndexValue).GetCellBoundaryVertices().ToArray();

        // Assert
        AssertCellBoundaryVertices(TestPointRes14BoundaryVertices, boundary);
    }

    [Test]
    public void Test_GetCellBoundary_PolygonWktMatchesPg() {
        // Arrange
        var geomFactory = new GeometryFactory(new PrecisionModel(1 / (EPSILON * 100)), 4236);

        // Act
        var polygon = new H3Index(TestHelpers.TestIndexValue).GetCellBoundary(geomFactory);

        // Assert
        Assert.That(polygon.Factory, Is.EqualTo(geomFactory), "should be using geomFactory not DefaultGeometryFactory");
        Assert.That(polygon.ToString(), Is.EqualTo(TestPointBoundaryPolygonWkt), "should be equal");
    }

    [Test]
    [TestCaseSource(typeof(H3GeometryExtensionsTests), nameof(GetCellBoundaryVerticesTestCases))]
    public bool Test_Upstream_GetCellBoundaryVertices(string testDataFn, List<(H3Index, (double Lat, double Lng)[])> expectedData) {
        // Arrange

        // Act
        var vertices = expectedData.Select(t => t.Item1.GetCellBoundaryVertices().ToList()).ToList();

        // Assert — expected vertices are authoritative libh3 degrees; hold ours to MaxUlps.
        for (var v = 0; v < expectedData.Count; v += 1) {
            var expectedVerts = expectedData[v].Item2;
            var actualVerts = vertices[v];

            if (expectedVerts.Length != actualVerts.Count) {
                Assert.Fail($"{testDataFn}: {expectedData[v].Item1} vertex count mismatch: expected {expectedVerts.Length} got {actualVerts.Count}");
                return false;
            }

            for (var i = 0; i < expectedVerts.Length; i += 1) {
                var ev = expectedVerts[i];
                var av = actualVerts[i];
                var okLat = TestHelpers.CheckUlps("Boundary.latDeg", av.LatitudeDegrees, ev.Lat);
                var okLng = TestHelpers.CheckUlps("Boundary.lngDeg", av.LongitudeDegrees, ev.Lng);
                if (!okLat || !okLng) {
                    Assert.Fail($"expected: {ev.Lat},{ev.Lng} actual: {av.LatitudeDegrees},{av.LongitudeDegrees} ulp: {TestHelpers.UlpDistance(av.LatitudeDegrees, ev.Lat)},{TestHelpers.UlpDistance(av.LongitudeDegrees, ev.Lng)}");
                    return false;
                }
            }

        }

        return true;
    }

    [Test]
    public void Test_GetCellAreaInKm2() {
        // Arrange
        LatLng c = new(0, 0);
        var indexes = Enumerable.Range(0, MAX_H3_RES + 1).Select(r => H3Index.FromLatLng(c, r)).ToArray();

        // Act
        var areas = indexes.Select(index => index.CellAreaInKmSquared()).ToArray();

        // Assert
        for (var i = 0; i < CellAreasInKm2.Length; i += 1) {
            Assert.That(TestHelpers.CheckUlps("CellArea.km2", areas[i], CellAreasInKm2[i]), Is.True, $"{indexes[i]} should be {CellAreasInKm2[i]} not {areas[i]} (ulp {TestHelpers.UlpDistance(areas[i], CellAreasInKm2[i])})");
        }
    }

    [Test]
    public void Test_CellsToMultiPolygon_DissolvesContiguousCells() {
        // Arrange
        var disk = TestHelpers.SfIndex.GridDiskDistances(1).Select(cell => cell.Index).ToList();

        // Act
        var actual = disk.CellsToMultiPolygon();

        // Assert
        Assert.That(actual.NumGeometries, Is.EqualTo(1), "should dissolve to a single polygon");
        Assert.That(((Polygon)actual.GetGeometryN(0)).NumInteriorRings, Is.EqualTo(0), "should have no holes");
        Assert.That(((Polygon)actual.GetGeometryN(0)).Shell.NumPoints - 1, Is.EqualTo(18), "outline should have 18 vertices");
    }

    [Test]
    public void Test_CellsToMultiPolygon_PreservesHoles() {
        // Arrange
        var donut = TestHelpers.SfIndex.GridDiskDistances(1)
            .Where(cell => cell.Distance == 1)
            .Select(cell => cell.Index)
            .ToList();

        // Act
        var actual = donut.CellsToMultiPolygon();

        // Assert
        Assert.That(actual.NumGeometries, Is.EqualTo(1), "should dissolve to a single polygon");
        Assert.That(((Polygon)actual.GetGeometryN(0)).NumInteriorRings, Is.EqualTo(1), "should have one hole");
    }

    [Test]
    public void Test_CellsToMultiPolygon_SeparatesDisjointCells() {
        // Arrange
        var cells = new[] { TestHelpers.SfIndex, H3Index.FromLatLng(new LatLng(0, 0), 9) };

        // Act
        var actual = cells.CellsToMultiPolygon();

        // Assert
        Assert.That(actual.NumGeometries, Is.EqualTo(2), "should produce two disjoint polygons");
    }

    [Test]
    public void Test_CellsToMultiPolygon_EmptyInput() {
        // Act
        var actual = Enumerable.Empty<H3Index>().CellsToMultiPolygon();

        // Assert
        Assert.That(actual.IsEmpty, Is.True, "should be empty");
    }

    [Test]
    public void Test_CellsToMultiPolygon_ThrowsOnInvalidCell() {
        // Arrange
        var cells = new[] { TestHelpers.SfIndex, H3Index.Invalid };

        // Act
        Action actual = () => cells.CellsToMultiPolygon();

        // Assert
        Assert.Throws<ArgumentException>(actual, "should throw for invalid cell");
    }

    [Test]
    public void Test_CellsToMultiPolygon_ThrowsOnMixedResolutions() {
        // Arrange
        var cells = new[] { TestHelpers.SfIndex, TestHelpers.SfIndex.GetParentForResolution(8) };

        // Act
        Action actual = () => cells.CellsToMultiPolygon();

        // Assert
        Assert.Throws<ArgumentException>(actual, "should throw for mixed resolutions");
    }

    [Test]
    public void Test_CellsToMultiPolygon_ThrowsOnDuplicates() {
        // Arrange
        var cells = new[] { TestHelpers.SfIndex, TestHelpers.SfIndex };

        // Act
        Action actual = () => cells.CellsToMultiPolygon();

        // Assert
        Assert.Throws<ArgumentException>(actual, "should throw for duplicate cells");
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Test_Upstream_CellAreas_SumToSphereArea(int resolution) {
        // Arrange
        var cells = H3Index.GetRes0Cells()
            .SelectMany(cell => cell.GetChildrenForResolution(resolution));

        // Act
        var sum = 0.0;
        var compensation = 0.0;
        foreach (var cell in cells) {
            var value = cell.CellAreaInRadiansSquared() - compensation;
            var t = sum + value;
            compensation = (t - sum) - value;
            sum = t;
        }

        // Assert
        Assert.That(TestHelpers.CheckUlps("SphereSum", sum, 4.0 * M_PI, TestHelpers.SphereSumMaxUlps), Is.True, $"cell areas should sum to the area of the sphere (ulp {TestHelpers.UlpDistance(sum, 4.0 * M_PI)})");
    }

    [Test]
    public void Test_GetFaces_HexagonWithEdgeVertices() {
        // Arrange
        // Class II pentagon neighbor - one face, two adjacent vertices on edge
        var index = new H3Index(0x821c37fffffffffUL);

        // Act
        var faces = index.GetFaces();

        // Assert
        Assert.That(CountValidFaces(faces), Is.EqualTo(1), "should have 1 face");
    }

    [Test]
    [TestCase(0x831c06fffffffffUL)]
    [TestCase(0x821ce7fffffffffUL)]
    public void Test_GetFaces_HexagonsWithTwoFaces(ulong index) {
        // Arrange
        var h3 = new H3Index(index);

        // Act
        var faces = h3.GetFaces();

        // Assert
        Assert.That(CountValidFaces(faces), Is.EqualTo(2), "should have 2 faces");
    }

    [Test]
    [TestCaseSource(nameof(PentagonFaceIndicies))]
    public void Test_GetFaces_Pentagons(H3Index index) {
        // Arrange
        var h3 = new H3Index(index);

        // Act
        var faces = h3.GetFaces();

        // Assert
        Assert.That(h3.IsPentagon, Is.True, "should be a pentagon");
        Assert.That(CountValidFaces(faces), Is.EqualTo(5), "should have 5 faces");
    }

    private static int CountValidFaces(int[] faces) => faces.Count(face => face is >= 0 and <= 19);

    private static void AssertCellBoundaryVertices(Point[] expected, LatLng[] actual) {
        Assert.That(actual.Length, Is.EqualTo(expected.Length), "should be same length");
        for (var i = 0; i < expected.Length; i += 1) {
            var p = expected[i];
            Assert.That(TestHelpers.CheckUlps("Boundary.lngDeg", actual[i].LongitudeDegrees, p.X), Is.True, $"longitude {i} should be {p.X} not {actual[i].LongitudeDegrees} (ulp {TestHelpers.UlpDistance(actual[i].LongitudeDegrees, p.X)})");
            Assert.That(TestHelpers.CheckUlps("Boundary.latDeg", actual[i].LatitudeDegrees, p.Y), Is.True, $"latitude {i} should be {p.Y} not {actual[i].LatitudeDegrees} (ulp {TestHelpers.UlpDistance(actual[i].LatitudeDegrees, p.Y)})");
        }

    }
}