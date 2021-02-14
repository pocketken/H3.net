using System;
using H3.Algorithms;
using H3.Extensions;
using static H3.Utils;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using GeoAPI.Geometries;
using System.Collections.Generic;
using H3.Model;
using System.Linq;
using NetTopologySuite.IO;

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
        public void Test_Polyfill_FillKnownValueBoundary() {
            // Arrange
            var h3 = new H3Index(TestHelpers.TestIndexValue);
            var boundary = h3.GetCellBoundary();

            // Act
            var filled = Polyfill.FillPolygon(boundary, 15).ToArray();

            // Assert
            Assert.AreEqual(KnownValuePolyfillAtRes15.Length, filled.Length, "should be same length");
            for (int i = 0; i < KnownValuePolyfillAtRes15.Length; i += 1) {
                Assert.Contains(KnownValuePolyfillAtRes15[i], filled, $"missing {KnownValuePolyfillAtRes15[i]}");
            }
        }

        [Test]
        public void Test_Polyfill_UberSfTestPoly() {
            // Arrange
            var polygon = DefaultGeometryFactory.CreatePolygon(
                UberSfTestPoly.Select(g => g.ToCoordinate()).ToArray());

            // Act
            var filled = Polyfill.FillPolygon(polygon, 9).ToArray();

            // Assert
            Assert.AreEqual(1253, filled.Length, "should return 1253 indicies");
        }

    }
}
