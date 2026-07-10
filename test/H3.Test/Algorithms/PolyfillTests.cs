using System.Linq;
using NUnit.Framework;

using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using static H3.Utils;
using NetTopologySuite.Geometries;

namespace H3.Test.Algorithms; 

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PolyfillTests {

    // coordinates for the upstream lib's "SF" test poly
    private static readonly LatLng[] UberSfTestPoly = {
        (0.659966917655, -2.1364398519396),
        (0.6595011102219, -2.1359434279405),
        (0.6583348114025, -2.1354884206045),
        (0.6581220034068, -2.1382437718946),
        (0.6594479998527, -2.1384597563896),
        (0.6599990002976, -2.1376771158464),
        (0.659966917655, -2.1364398519396)
    };

    // upstream lib's prime meridian test poly
    private static readonly LatLng[] PrimeMeridianVerts = {
        (0.01, 0.01), (0.01, -0.01), (-0.01, -0.01), (-0.01, 0.01), (0.01, 0.01)
    };

    // upstream lib's trans meridian test poly/holes
    private static readonly LatLng[] TransMeridianVerts = {
        (0.01, -M_PI + 0.01),
        (0.01, M_PI - 0.01),
        (-0.01, M_PI - 0.01),
        (-0.01, -M_PI + 0.01),
        (0.01, -M_PI + 0.01)
    };
    private static readonly LatLng[] TransMeridianHoleVerts = {
        (0.005, -M_PI + 0.005),
        (0.005, M_PI - 0.005),
        (-0.005, M_PI - 0.005),
        (-0.005, -M_PI + 0.005),
        (0.005, -M_PI + 0.005),
    };
    private static readonly LatLng[] TransMeridianComplexVerts = {
        (0.1, -M_PI + 0.00001),
        (0.1, M_PI - 0.00001),
        (0.05, M_PI - 0.2),
        (-0.1, M_PI - 0.00001),
        (-0.1, -M_PI + 0.00001),
        (-0.05, -M_PI + 0.2),
        (0.1, -M_PI + 0.00001)
    };

    // upstream lib's "entire world" test poly
    private static readonly LatLng[] EntireWorld = {
        (M_PI_2, -M_PI),
        (M_PI_2, M_PI),
        (-M_PI_2, M_PI),
        (-M_PI_2, -M_PI),
        (M_PI_2, -M_PI),
    };

    // select h3_polyfill(h3_to_geo_boundary_geography('8e48e1d7038d527'::h3index), 15);
    private static readonly H3Index[] KnownValuePolyfillAtRes15 = new H3Index[7] {
        0x8f48e1d7038d520,
        0x8f48e1d7038d521,
        0x8f48e1d7038d522,
        0x8f48e1d7038d523,
        0x8f48e1d7038d524,
        0x8f48e1d7038d525,
        0x8f48e1d7038d526
    };

    [Test]
    public void Test_Polyfill_Empty() {
        // Act
        var filled = DefaultGeometryFactory.CreatePolygon().Fill(5);

        // Assert
        Assert.That(filled, Is.Empty, "should be empty");
    }

    [Test]
    public void Test_Polyfill_Exact() {
        // Arrange
        var index = H3Index.FromLatLng((1, 2), 9);
        var boundary = index.GetCellBoundary();

        // Act
        var filled = boundary.Fill(9).ToArray();

        // Assert
        Assert.That(filled.Length, Is.EqualTo(1), "should return 1 index");
        Assert.That(index == filled[0], Is.True, $"should be index {index} not {filled[0]}");
    }

    [Test]
    public void Test_Polyfill_FillKnownValueBoundary() {
        // Arrange
        var h3 = new H3Index(TestHelpers.TestIndexValue);
        var boundary = h3.GetCellBoundary();

        // Act
        var filled = boundary.Fill(15).ToArray();

        // Assert
        Assert.That(filled.Length, Is.EqualTo(KnownValuePolyfillAtRes15.Length), "should be same length");
        for (var i = 0; i < KnownValuePolyfillAtRes15.Length; i += 1) {
            Assert.That(filled, Does.Contain(KnownValuePolyfillAtRes15[i]), $"missing {KnownValuePolyfillAtRes15[i]}");
        }
    }

    [Test]
    public void Test_Polyfill_UberSfTestPoly() {
        // Arrange
        var polygon = CreatePolygon(UberSfTestPoly);

        // Act
        var filledCount = polygon.Fill(9).Count();

        // Assert
        Assert.That(filledCount, Is.EqualTo(1253), "should return 1253 indicies");
    }

    [Test]
    public void Test_Polyfill_VertexNodeAny_UberSfTestPoly() {
        // Arrange
        var polygon = CreatePolygon(UberSfTestPoly);

        // Act
        var filledCount = polygon.Fill(9, VertexTestMode.Any).Count();

        // Assert
        Assert.That(filledCount, Is.EqualTo(1334), "should return 1334 indicies");
    }

    [Test]
    public void Test_Polyfill_VertexNodeAll_UberSfTestPoly() {
        // Arrange
        var polygon = CreatePolygon(UberSfTestPoly);

        // Act
        var filledCount = polygon.Fill(9, VertexTestMode.All).Count();

        // Assert
        Assert.That(filledCount, Is.EqualTo(1175), "should return 1175 indicies");
    }

    [Test]
    public void Test_Polyfill_PrimeMeridian() {
        // Arrange
        var polygon = CreatePolygon(PrimeMeridianVerts);

        // Act
        var filledCount = polygon.Fill(7).Count();

        // Assert
        Assert.That(filledCount, Is.EqualTo(4228), "should return 4228 indicies");
    }

    [Test]
    public void Test_Polyfill_TransMeridian() {
        // Arrange
        var polygon = CreatePolygon(TransMeridianVerts);

        // Act
        var filledCount = polygon.Fill(7).Count();

        // Assert
        Assert.That(filledCount, Is.EqualTo(4238), "should return 4328 indicies");
    }

    [Test]
    public void Test_Polyfill_TransMeridianWithHole() {
        // Arrange
        var polygon = CreatePolygonWithHole(TransMeridianVerts, TransMeridianHoleVerts);

        // Act
        var filledCount = polygon.Fill(7).Count();

        // Assert
        Assert.That(filledCount, Is.EqualTo(3176), "should return 3176 indicies");
    }

    [Test]
    public void Test_Polyfill_TransMeridianComplex() {
        // Arrange
        var polygon = CreatePolygon(TransMeridianComplexVerts);

        // Act
        var filledCount = polygon.Fill(4).Count();

        // Assert
        Assert.That(filledCount, Is.EqualTo(1204), "should return 1204 indicies");
    }

    private const double EdgeLength2 = 0.001 * M_PI_180;

    [Test]
    public void Test_Polyfill_Pentagon() {
        // Arrange
        var index = H3Index.Create(9, 24, 0);
        var coord = index.ToLatLng();
        LatLng topRight = new() {
            Latitude = coord.Latitude + EdgeLength2,
            Longitude = coord.Longitude + EdgeLength2
        };
        LatLng topLeft = new() {
            Latitude = coord.Latitude + EdgeLength2,
            Longitude = coord.Longitude - EdgeLength2
        };
        LatLng bottomRight = new() {
            Latitude = coord.Latitude - EdgeLength2,
            Longitude = coord.Longitude + EdgeLength2
        };
        LatLng bottomLeft = new() {
            Latitude = coord.Latitude - EdgeLength2,
            Longitude = coord.Longitude - EdgeLength2
        };
        var polygon = CreatePolygon(new[] { topLeft, topRight, bottomRight, bottomLeft, topLeft });

        // Act
        var filled = polygon.Fill(9).ToArray();

        // Assert
        Assert.That(filled.Length, Is.EqualTo(1), "should return 1 index");
        Assert.That(filled[0].IsPentagon, Is.True, "should be a pentagon index");
    }

    [Test]
    [TestCase(0, 122)]
    [TestCase(1, 842)]
    [TestCase(2, 5882)]
    [TestCase(3, 41162)]
    [TestCase(4, 288122)]
    public void Test_Polyfill_EntireWorldMuhahaha(int resolution, int expectedCount) {
        // Arrange
        var polygon = CreatePolygon(EntireWorld);

        // Act
        var filled = polygon.Fill(resolution).Count();

        // Assert
        Assert.That(filled, Is.EqualTo(expectedCount), $"should have filled {expectedCount}");
    }

    [Test]
    public void Test_Upstream_H3jsIssue67_One() {
        // Arrange
        var east = -56.25 * M_PI_180;
        var north = -33.13755119234615 * M_PI_180;
        var south = -34.30714385628804 * M_PI_180;
        var west = -57.65625 * M_PI_180;

        var polygon = CreatePolygon(new LatLng[] {
            (north, east),
            (south, east),
            (south, west),
            (north, west),
            (north, east)
        });

        // Act
        var filled = polygon.Fill(7).Count();

        // Arrange
        Assert.That(filled, Is.EqualTo(4499), "should have filled 4499");
    }

    [Test]
    public void Test_Upstream_H3jsIssue67_Two() {
        // Arrange
        var east = -57.65625 * M_PI_180;
        var north = -34.30714385628804 * M_PI_180;
        var south = -35.4606699514953 * M_PI_180;
        var west = -59.0625 * M_PI_180;

        var polygon = CreatePolygon(new LatLng[] {
            (north, east),
            (south, east),
            (south, west),
            (north, west),
            (north, east)
        });

        // Act
        var filled = polygon.Fill(7).Count();

        // Arrange
        Assert.That(filled, Is.EqualTo(4609), "should have filled 4499");
    }

    [Test]
    public void Test_Upstream_H3jsIssue136() {
        // Arrange
        LatLng[] testVerts = {
            (0.10068990369902957, 0.8920772174196191),
            (0.10032914690616246, 0.8915914753447348),
            (0.10033349237998787, 0.8915860128746426),
            (0.10069496685903621, 0.8920742194546231),
            (0.10068990369902957, 0.8920772174196191)
        };
        var polygon = CreatePolygon(testVerts);

        // Act
        var filled = polygon.Fill(13).Count();

        // Arrange
        Assert.That(filled, Is.EqualTo(4353), "should have filled 4353");
    }

    [Test]
    public void Test_Upstream_H3Issue595_Res10() {
        // Arrange
        LatLng[] verts = {
            (51.51746913670682, -0.1251052178242358),
            (51.51981162007689, -0.1254345584085245),
            (51.522082928275104, -0.1264125733143912),
            (51.52421404875121, -0.1280095460538208),
            (51.52614022848794, -0.1301769534182327),
            (51.52780294148869, -0.1328489398316205),
            (51.52915166706029, -0.1359443183389036),
            (51.53014542485875, -0.1393690374304754),
            (51.53075402005622, -0.1430190387497298),
            (51.5309589607956, -0.1467834188535714),
            (51.53075402005622, -0.1505477989574129),
            (51.53014542485875, -0.1541978002766674),
            (51.52915166706029, -0.1576225193682391),
            (51.52780294148869, -0.1607178978755222),
            (51.52614022848794, -0.16338988428891),
            (51.52421404875121, -0.1655572916533219),
            (51.522082928275104, -0.1671542643927515),
            (51.51981162007689, -0.1681322792986182),
            (51.51746913670682, -0.1684616198829069),
            (51.51512665333676, -0.1681322792986182),
            (51.51285534513854, -0.1671542643927515),
            (51.51072422466243, -0.1655572916533219),
            (51.508798044925705, -0.16338988428891),
            (51.50713533192496, -0.1607178978755222),
            (51.505786606353354, -0.1576225193682392),
            (51.50479284855489, -0.1541978002766674),
            (51.50418425335742, -0.1505477989574129),
            (51.50397931261804, -0.1467834188535714),
            (51.50418425335742, -0.1430190387497299),
            (51.50479284855489, -0.1393690374304753),
            (51.505786606353354, -0.1359443183389036),
            (51.50713533192496, -0.1328489398316205),
            (51.508798044925705, -0.1301769534182327),
            (51.51072422466243, -0.1280095460538208),
            (51.51285534513854, -0.1264125733143912),
            (51.51512665333676, -0.1254345584085245),
            (51.51746913670682, -0.1251052178242358)
        };
        var polygon =
            CreatePolygon(verts.Select(v => new LatLng(v.Latitude * M_PI_180, v.Longitude * M_PI_180)).ToArray());
        var expectedCenter = H3Index.FromPoint(polygon.Centroid, 10);

        // Act
        var filled = polygon.Fill(10).ToList();

        // Assert
        Assert.That(filled, Does.Contain(expectedCenter), "should contain polygon centroid");
    }

    [Test]
    public void Test_Upstream_h3Issue595_Res9() {
        // Arrange
        LatLng[] verts = {
            (51.51746913670682, -0.1251052178242358),
            (51.51981162007689, -0.1254345584085245),
            (51.522082928275104, -0.1264125733143912),
            (51.52421404875121, -0.1280095460538208),
            (51.52614022848794, -0.1301769534182327),
            (51.52780294148869, -0.1328489398316205),
            (51.52915166706029, -0.1359443183389036),
            (51.53014542485875, -0.1393690374304754),
            (51.53075402005622, -0.1430190387497298),
            (51.5309589607956, -0.1467834188535714),
            (51.53075402005622, -0.1505477989574129),
            (51.53014542485875, -0.1541978002766674),
            (51.52915166706029, -0.1576225193682391),
            (51.52780294148869, -0.1607178978755222),
            (51.52614022848794, -0.16338988428891),
            (51.52421404875121, -0.1655572916533219),
            (51.522082928275104, -0.1671542643927515),
            (51.51981162007689, -0.1681322792986182),
            (51.51746913670682, -0.1684616198829069),
            (51.51512665333676, -0.1681322792986182),
            (51.51285534513854, -0.1671542643927515),
            (51.51072422466243, -0.1655572916533219),
            (51.508798044925705, -0.16338988428891),
            (51.50713533192496, -0.1607178978755222),
            (51.505786606353354, -0.1576225193682392),
            (51.50479284855489, -0.1541978002766674),
            (51.50418425335742, -0.1505477989574129),
            (51.50397931261804, -0.1467834188535714),
            (51.50418425335742, -0.1430190387497299),
            (51.50479284855489, -0.1393690374304753),
            (51.505786606353354, -0.1359443183389036),
            (51.50713533192496, -0.1328489398316205),
            (51.508798044925705, -0.1301769534182327),
            (51.51072422466243, -0.1280095460538208),
            (51.51285534513854, -0.1264125733143912),
            (51.51512665333676, -0.1254345584085245),
            (51.51746913670682, -0.1251052178242358)
        };
        var polygon =
            CreatePolygon(verts.Select(v => new LatLng(v.Latitude * M_PI_180, v.Longitude * M_PI_180)).ToArray());
        var expectedCenter = H3Index.FromPoint(polygon.Centroid, 9);

        // Act
        var filled = polygon.Fill(9).ToList();

        // Assert
        Assert.That(filled, Does.Contain(expectedCenter), "should contain polygon centroid");
        Assert.That(filled.Count, Is.EqualTo(77), "should have 77 cells");
    }

    [Test]
    public void Test_GH53_Polyfill_DisjointMultiPolygon_FillsAllComponents() {
        // Arrange
        var sf = CreatePolygon(UberSfTestPoly);
        var eq = H3Index.FromLatLng((1, 2), 9).GetCellBoundary();
        var multi = DefaultGeometryFactory.CreateMultiPolygon(new[] { sf, eq });
        var expected = sf.Fill(9).Concat(eq.Fill(9)).Distinct().ToHashSet();

        // Act
        var filled = multi.Fill(9).ToHashSet();

        // Assert
        Assert.That(filled, Is.EquivalentTo(expected), "should fill every disjoint component");
    }

    [Test]
    public void Test_GH53_Polyfill_GeometryCollection() {
        // Arrange
        var polygon = CreatePolygon(UberSfTestPoly);
        var point = H3Index.FromLatLng((1, 2), 9).ToPoint();
        var collection = DefaultGeometryFactory.CreateGeometryCollection(new Geometry[] { polygon, point });
        var expected = polygon.Fill(9).Concat(point.Fill(9)).Distinct().ToHashSet();

        // Act
        var filled = collection.Fill(9).ToHashSet();

        // Assert
        Assert.That(filled, Is.EquivalentTo(expected), "should fill union of collection members");
    }

    [Test]
    public void Test_GH53_Polyfill_Point() {
        // Arrange
        var index = H3Index.FromLatLng((1, 2), 9);

        // Act
        var filled = index.ToPoint().Fill(9).ToArray();

        // Assert
        Assert.That(filled, Is.EqualTo(new[] { index }), "should fill to the containing cell");
    }

    [Test]
    public void Test_GH53_Polyfill_PredicateOverload_MatchesCenterMode() {
        // Arrange
        var polygon = CreatePolygon(UberSfTestPoly);
        var locator = new NetTopologySuite.Algorithm.Locate.IndexedPointInAreaLocator(polygon);
        var expected = polygon.Fill(9).ToHashSet();

        // Act
        var filled = polygon.Fill(9, index => locator.Locate(index.ToCoordinate()) == Location.Interior).ToHashSet();

        // Assert
        Assert.That(filled, Is.EquivalentTo(expected), "predicate fill should match center vertex mode");
    }

    [Test]
    public void Test_GH53_Upstream521_VertexLatitudeMatchesCellCenter() {
        // Arrange
        var index = H3Index.FromLatLng((0.659966917655, -2.1364398519396), 9);
        var center = index.ToLatLng();
        LatLng[] verts = {
            (center.Latitude, center.Longitude - 0.001),
            (center.Latitude + 0.001, center.Longitude),
            (center.Latitude, center.Longitude + 0.001),
            (center.Latitude - 0.001, center.Longitude),
            (center.Latitude, center.Longitude - 0.001)
        };
        var polygon = CreatePolygon(verts);

        // Act
        var filled = polygon.Fill(9).ToList();

        // Assert
        Assert.That(filled, Does.Contain(index), "should contain the cell whose center latitude matches polygon vertices");
    }

    /// <summary>
    /// Creates a new polygon, with vertices in CCW order.
    /// </summary>
    /// <param name="verts"></param>
    /// <returns></returns>
    private static Polygon CreatePolygon(LatLng[] verts) =>
        DefaultGeometryFactory.CreatePolygon(verts.Select(g => g.ToCoordinate()).Reverse().ToArray());

    /// <summary>
    /// Creates a new polygon with a hole, with vertices in CCW order.
    /// </summary>
    /// <param name="verts"></param>
    /// <param name="holeVerts"></param>
    /// <returns></returns>
    private static Polygon CreatePolygonWithHole(LatLng[] verts, LatLng[] holeVerts) =>
        DefaultGeometryFactory.CreatePolygon(
            DefaultGeometryFactory.CreateLinearRing(verts.Select(g => g.ToCoordinate()).Reverse().ToArray()),
            new[] { DefaultGeometryFactory.CreateLinearRing(holeVerts.Select(g => g.ToCoordinate()).Reverse().ToArray()) }
        );
}