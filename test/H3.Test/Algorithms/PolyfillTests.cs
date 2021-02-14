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

namespace H3.Test.Algorithms {

    [TestFixture]
    public class PolyfillTests {

        public static GeoCoord[] UberSfTestPoly = new GeoCoord[] {
            (0.659966917655, -2.1364398519396),
            (0.6595011102219, -2.1359434279405),
            (0.6583348114025, -2.1354884206045),
            (0.6581220034068, -2.1382437718946),
            (0.6594479998527, -2.1384597563896),
            (0.6599990002976, -2.1376771158464),
            (0.659966917655, -2.1364398519396)
        };

        [Test]
        public void Test_Polyfill_FillPolygon() {
            // Arrange
            var h3 = new H3Index(TestHelpers.TestIndexValue);
            var boundary = h3.GetCellBoundary();

            // Act
            var filled = Polyfill.FillPolygon(boundary, 15);
            List<IPolygon> polys = new();
            polys.Add(boundary);
            foreach (var index in filled) {
                polys.Add(index.GetCellBoundary());
            }
            var mp = DefaultGeometryFactory.CreateMultiPolygon(polys.ToArray());
            Console.WriteLine($"{mp}");

            // Assert
            Assert.IsNotEmpty(filled, "should not be empty");
        }

        [Test]
        public void Test_Polyfill_UberSfTestPoly() {
            // Arrange
            var polygon = DefaultGeometryFactory.CreatePolygon(
                UberSfTestPoly.Select(g => g.ToCoordinate()).ToArray());

            // Act
            var filled = Polyfill.FillPolygon(polygon, 7);
            List<IPolygon> polys = new();
            polys.Add(polygon);
            foreach (var index in filled) {
                polys.Add(index.GetCellBoundary());
            }
            var mp = DefaultGeometryFactory.CreateMultiPolygon(polys.ToArray());
            Console.WriteLine($"{mp}");

            // Assert
        }
    }
}
