using System;
using System.Linq;
using NUnit.Framework;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using static H3.Utils;
using NetTopologySuite.Geometries;

namespace H3.Test.Algorithms {

    [TestFixture]
    public class PolyfillTests {

        // coordinates for the upstream lib's "SF" test poly
        private static readonly GeoCoord[] UberSfTestPoly = new GeoCoord[] {
            (0.659966917655, -2.1364398519396),
            (0.6595011102219, -2.1359434279405),
            (0.6583348114025, -2.1354884206045),
            (0.6581220034068, -2.1382437718946),
            (0.6594479998527, -2.1384597563896),
            (0.6599990002976, -2.1376771158464),
            (0.659966917655, -2.1364398519396)
        };

        // upstream lib's prime meridian test poly
        private static readonly GeoCoord[] PrimeMeridianVerts = new GeoCoord[] {
             (0.01, 0.01), (0.01, -0.01), (-0.01, -0.01), (-0.01, 0.01), (0.01, 0.01)
        };

        // upstream lib's trans meridian test poly/holes
        private static readonly GeoCoord[] TransMeridianVerts = new GeoCoord[] {
            (0.01, -M_PI + 0.01),
            (0.01, M_PI - 0.01),
            (-0.01, M_PI - 0.01),
            (-0.01, -M_PI + 0.01),
            (0.01, -M_PI + 0.01)
        };
        private static readonly GeoCoord[] TransMeridianHoleVerts = new GeoCoord[] {
            (0.005, -M_PI + 0.005),
            (0.005, M_PI - 0.005),
            (-0.005, M_PI - 0.005),
            (-0.005, -M_PI + 0.005),
            (0.005, -M_PI + 0.005),
        };
        private static readonly GeoCoord[] TransMeridianComplexVerts = new GeoCoord[] {
            (0.1, -M_PI + 0.00001),
            (0.1, M_PI - 0.00001),
            (0.05, M_PI - 0.2),
            (-0.1, M_PI - 0.00001),
            (-0.1, -M_PI + 0.00001),
            (-0.05, -M_PI + 0.2),
            (0.1, -M_PI + 0.00001)
        };

        // upstream lib's "entire world" test poly
        private static readonly GeoCoord[] EntireWorld = new GeoCoord[] {
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
            Assert.IsEmpty(filled, "should be empty");
        }

        [Test]
        public void Test_Polyfill_Exact() {
            // Arrange
            var index = H3Index.FromGeoCoord((1, 2), 9);
            var boundary = index.GetCellBoundary();

            // Act
            var filled = boundary.Fill(9).ToArray();

            // Assert
            Assert.AreEqual(1, filled.Length, "should return 1 index");
            Assert.IsTrue(index == filled[0], $"should be index {index} not {filled[0]}");
        }

        [Test]
        public void Test_Polyfill_FillKnownValueBoundary() {
            // Arrange
            var h3 = new H3Index(TestHelpers.TestIndexValue);
            var boundary = h3.GetCellBoundary();

            // Act
            var filled = boundary.Fill(15).ToArray();

            // Assert
            Assert.AreEqual(KnownValuePolyfillAtRes15.Length, filled.Length, "should be same length");
            for (int i = 0; i < KnownValuePolyfillAtRes15.Length; i += 1) {
                Assert.Contains(KnownValuePolyfillAtRes15[i], filled, $"missing {KnownValuePolyfillAtRes15[i]}");
            }
        }

        [Test]
        public void Test_Polyfill_UberSfTestPoly() {
            // Arrange
            var polygon = CreatePolygon(UberSfTestPoly);

            // Act
            var filledCount = polygon.Fill(9).Count();

            // Assert
            Assert.AreEqual(1253, filledCount, "should return 1253 indicies");
        }

        [Test]
        public void Test_Polyfill_PrimeMeridian() {
            // Arrange
            var polygon = CreatePolygon(PrimeMeridianVerts);

            // Act
            var filledCount = polygon.Fill(7).Count();

            // Assert
            Assert.AreEqual(4228, filledCount, "should return 4228 indicies");
        }

        [Test]
        public void Test_Polyfill_TransMeridian() {
            // Arrange
            var polygon = CreatePolygon(TransMeridianVerts);

            // Act
            var filledCount = polygon.Fill(7).Count();

            // Assert
            Assert.AreEqual(4238, filledCount, "should return 4328 indicies");
        }

        [Test]
        public void Test_Polyfill_TransMeridianWithHole() {
            // Arrange
            var polygon = CreatePolygonWithHole(TransMeridianVerts, TransMeridianHoleVerts);

            // Act
            var filledCount = polygon.Fill(7).Count();

            // Assert
            Assert.AreEqual(3176, filledCount, "should return 3176 indicies");
        }

        [Test]
        public void Test_Polyfill_TransMeridianComplex() {
            // Arrange
            var polygon = CreatePolygon(TransMeridianComplexVerts);

            // Act
            var filledCount = polygon.Fill(4).Count();

            // Assert
            Assert.AreEqual(1204, filledCount, "should return 1204 indicies");
        }

        private const double EdgeLength2 = 0.001 * M_PI_180;

        [Test]
        public void Test_Polyfill_Pentagon() {
            // Arrange
            var index = H3Index.Create(9, 24, 0);
            GeoCoord coord = index.ToGeoCoord();
            GeoCoord topRight = new GeoCoord {
                Latitude = coord.Latitude + EdgeLength2,
                Longitude = coord.Longitude + EdgeLength2
            };
            GeoCoord topLeft = new GeoCoord {
                Latitude = coord.Latitude + EdgeLength2,
                Longitude = coord.Longitude - EdgeLength2
            };
            GeoCoord bottomRight = new GeoCoord {
                Latitude = coord.Latitude - EdgeLength2,
                Longitude = coord.Longitude + EdgeLength2
            };
            GeoCoord bottomLeft = new GeoCoord {
                Latitude = coord.Latitude - EdgeLength2,
                Longitude = coord.Longitude - EdgeLength2
            };
            var polygon = CreatePolygon(new[] { topLeft, topRight, bottomRight, bottomLeft, topLeft });

            // Act
            var filled = polygon.Fill(9).ToArray();

            // Assert
            Assert.AreEqual(1, filled.Length, "should return 1 index");
            Assert.IsTrue(filled[0].IsPentagon, "should be a pentagon index");
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
            Assert.AreEqual(expectedCount, filled, $"should have filled {expectedCount}");
        }

        [Test]
        public void Test_Upstream_H3jsIssue67_One() {
            // Arrange
            double east = -56.25 * M_PI_180;
            double north = -33.13755119234615 * M_PI_180;
            double south = -34.30714385628804 * M_PI_180;
            double west = -57.65625 * M_PI_180;

            var polygon = CreatePolygon(new GeoCoord[] {
                (north, east),
                (south, east),
                (south, west),
                (north, west),
                (north, east)
            });

            // Act
            var filled = polygon.Fill(7).Count();

            // Arrange
            Assert.AreEqual(4499, filled, $"should have filled 4499");
        }

        [Test]
        public void Test_Upstream_H3jsIssue67_Two() {
            // Arrange
            double east = -57.65625 * M_PI_180;
            double north = -34.30714385628804 * M_PI_180;
            double south = -35.4606699514953 * M_PI_180;
            double west = -59.0625 * M_PI_180;

            var polygon = CreatePolygon(new GeoCoord[] {
                (north, east),
                (south, east),
                (south, west),
                (north, west),
                (north, east)
            });

            // Act
            var filled = polygon.Fill(7).Count();

            // Arrange
            Assert.AreEqual(4609, filled, $"should have filled 4499");
        }

        [Test]
        public void Test_Upstream_H3jsIssue136() {
            // Arrange
            GeoCoord[] testVerts = {
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
            Assert.AreEqual(4353, filled, $"should have filled 4353");
        }

        /// <summary>
        /// Creates a new polygon, with vertices in CCW order.
        /// </summary>
        /// <param name="verts"></param>
        /// <returns></returns>
        private static Polygon CreatePolygon(GeoCoord[] verts) =>
            DefaultGeometryFactory.CreatePolygon(verts.Select(g => g.ToCoordinate()).Reverse().ToArray());

        /// <summary>
        /// Creates a new polygon with a hole, with vertices in CCW order.
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="holeVerts"></param>
        /// <returns></returns>
        private static Polygon CreatePolygonWithHole(GeoCoord[] verts, GeoCoord[] holeVerts) =>
            DefaultGeometryFactory.CreatePolygon(
                    DefaultGeometryFactory.CreateLinearRing(verts.Select(g => g.ToCoordinate()).Reverse().ToArray()),
                    new[] { DefaultGeometryFactory.CreateLinearRing(holeVerts.Select(g => g.ToCoordinate()).Reverse().ToArray()) }
                );
    }
}
