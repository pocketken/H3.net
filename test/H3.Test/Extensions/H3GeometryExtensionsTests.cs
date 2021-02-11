using System;
using System.Linq;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NUnit.Framework;
using NetTopologySuite.Geometries;

namespace H3.Test.Extensions {

    [TestFixture]
    public class H3GeometryExtensionsTests {

        public static readonly H3Index[] PentagonFaceIndicies = new H3Index[] {
            TestHelpers.CreateIndex(1, 4, 0),
            TestHelpers.CreateIndex(2, 4, 0),
            TestHelpers.CreateIndex(15, 4, 0)
        };

        public static readonly double[] CellAreasInKm2 = new double[] {
            2.562182162955496e+06, 4.476842018179411e+05, 6.596162242711056e+04,
            9.228872919002590e+03, 1.318694490797110e+03, 1.879593512281298e+02,
            2.687164354763186e+01, 3.840848847060638e+00, 5.486939641329893e-01,
            7.838600808637444e-02, 1.119834221989390e-02, 1.599777169186614e-03,
            2.285390931423380e-04, 3.264850232091780e-05, 4.664070326136774e-06,
            6.662957615868888e-07
        };

        // select st_astext(h3_to_geo_boundary_geometry('8075fffffffffff'::h3index));
        // and discarding final polygon point
        public static readonly Point[] Res0BoundaryVertices = new Point[] {
            new Point(-4.01399844347047, 11.5452959754148),
            new Point( -13.708146703918, 6.27096513627577),
            new Point( -11.6647475421264, -4.46703160978452),
            new Point(-0.782839175105521, -5.88992175431391),
            new Point(3.94303615578645, 3.96879697660958),
        };

        // select st_astext(h3_to_geo_boundary_geometry('8e48e1d7038d527'::h3index));
        // and discarding final polygon point
        public static readonly Point[] TestPointRes14BoundaryVertices = new Point[] {
            new Point(-110.000000429101, 29.9999892327449),
            new Point(-109.99998660383, 29.9999986861296),
            new Point(-109.999989152051, 30.0000137159278),
            new Point(-110.000005525548, 30.0000192923406),
            new Point(-110.00001935082, 30.0000098389545),
            new Point(-110.000016802594, 29.9999948091569)
        };

        [Test]
        public void Test_GetCellBoundaryVertices_AtRes0() {
            // Arrange
            GeoCoord c = new GeoCoord(0, 0);
            var index = H3Index.FromGeoCoord(c, 0);

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
        public void Test_GetCellAreaInKm2() {
            // Arrange
            GeoCoord c = new GeoCoord(0, 0);
            var indexes = Enumerable.Range(0, MAX_H3_RES).Select(r => H3Index.FromGeoCoord(c, r)).ToArray();

            // Act
            var areas = indexes.Select(index => index.CellAreaInKmSquared()).ToArray();

            // Assert
            for (int i = 0; i < CellAreasInKm2.Length; i += 1) {
                Assert.IsTrue(Math.Abs(areas[i] - CellAreasInKm2[i]) < 1e-8, $"{indexes[i]} should be {CellAreasInKm2[i]} not {areas[i]}");
            }
        }

        [Test]
        public void Test_GetFaces_HexagonWithEdgeVertices() {
            // Arrange
            // Class II pentagon neighbor - one face, two adjacent vertices on edge
            var index = new H3Index(0x821c37fffffffffUL);

            // Act
            var faces = index.GetFaces();

            // Assert
            Assert.AreEqual(1, CountValidFaces(faces), "should have 1 face");
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
            Assert.AreEqual(2, CountValidFaces(faces), "should have 2 faces");
        }

        [Test]
        [TestCaseSource(nameof(PentagonFaceIndicies))]
        public void Test_GetFaces_Pentagons(H3Index index) {
            // Arrange
            var h3 = new H3Index(index);

            // Act
            var faces = h3.GetFaces();

            // Assert
            Assert.IsTrue(h3.IsPentagon, "should be a pentagon");
            Assert.AreEqual(5, CountValidFaces(faces), "should have 5 faces");
        }

        private static int CountValidFaces(int[] faces) => faces
            .Where(face => face >= 0 && face <= 19)
            .Count();

        private static void AssertCellBoundaryVertices(Point[] expected, GeoCoord[] actual) {
            Assert.AreEqual(expected.Length, actual.Length, "should be same length");
            for (int i = 0; i < expected.Length; i += 1) {
                var p = expected[i];
                Assert.IsTrue(Math.Abs(p.X - actual[i].LongitudeDegrees) < EPSILON_DEG, $"longitude {i} should be {p.X} not {actual[i].LongitudeDegrees}");
                Assert.IsTrue(Math.Abs(p.Y - actual[i].LatitudeDegrees) < EPSILON_DEG, $"latitude {i} should be {p.X} not {actual[i].LongitudeDegrees}");
            }

        }
    }

}
