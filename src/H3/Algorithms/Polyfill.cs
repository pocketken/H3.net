using System;
using System.Collections.Generic;
using System.Linq;
using H3.Extensions;
using H3.Model;
using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;

#nullable enable

namespace H3.Algorithms {

    internal sealed class PositiveLonFilter : ICoordinateSequenceFilter {
        public bool Done => false;

        public bool GeometryChanged => true;

        public void Filter(CoordinateSequence seq, int i) {
            double x = seq.GetX(i);
            seq.SetOrdinate(i, Ordinate.X, x < 0 ? x + 360.0 : x);
        }
    }

    internal sealed class NegativeLonFilter : ICoordinateSequenceFilter {
        public bool Done => false;

        public bool GeometryChanged => true;

        public void Filter(CoordinateSequence seq, int i) {
            double x = seq.GetX(i);
            seq.SetOrdinate(i, Ordinate.X, x > 0 ? x - 360.0 : x);
        }
    }

    /// <summary>
    /// Polyfill algorithms for H3Index.
    /// </summary>
    public static class Polyfill {

        private static readonly ICoordinateSequenceFilter _negativeLonFilter = new NegativeLonFilter();

        private static readonly ICoordinateSequenceFilter _positiveLonFilter = new PositiveLonFilter();

        /// <summary>
        /// Returns all of the H3 indexes that are contained within the provided
        /// Polygon at the specified resolution.  Supports Polygons with holes.
        /// </summary>
        /// <param name="polygon">Containment polygon</param>
        /// <param name="resolution">H3 resolution</param>
        /// <returns>Indicies where center point is contained within polygon</returns>
        public static IEnumerable<H3Index> Fill(this Geometry polygon, int resolution) {
            bool isTransMeridian = polygon.IsTransMeridian();
            var testPoly = isTransMeridian ? SplitGeometry(polygon) : polygon;

            HashSet<ulong> searched = new();

            Stack<H3Index> toSearch = new(GetIndicies(testPoly.Coordinates, resolution));
            if (toSearch.Count == 0 && !testPoly.IsEmpty) {
                toSearch.Push(H3Index.FromCoordinate(testPoly.InteriorPoint.Coordinate, resolution));
            }

            IndexedPointInAreaLocator locator = new(testPoly);
            var coordinate = new Coordinate();

            while (toSearch.Count != 0) {
                var index = toSearch.Pop();

                foreach (var neighbour in index.GetNeighbours()) {
                    if (searched.Contains(neighbour)) continue;
                    searched.Add(neighbour);
                    var geoCoord = neighbour.ToGeoCoord();
                    coordinate.X = geoCoord.LongitudeDegrees;
                    coordinate.Y = geoCoord.LatitudeDegrees;
                    var location = locator.Locate(coordinate);

                    if (location != Location.Interior)
                        continue;

                    yield return neighbour;
                    toSearch.Push(neighbour);
                }
            }
        }

        /// <summary>
        /// Returns all of the H3 indexes that follow the provided LineString
        /// at the specified resolution.
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static IEnumerable<H3Index> Fill(this LineString polyline, int resolution) =>
            polyline.Coordinates.GetIndicies(resolution);

        /// <summary>
        /// Gets all of the H3 indices that define the provided set of Coordinates.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static IEnumerable<H3Index> GetIndicies(this Coordinate[] coordinates, int resolution) {
            HashSet<H3Index> indicies = new();

            // trace the coordinates
            int coordLen = coordinates.Length - 1;
            for (int c = 0; c < coordLen; c += 1) {
                // from this coordinate to next/first
                var vertA = coordinates[c];
                var vertB = coordinates[c + 1];

                // estimate number of indicies between points, use that as a
                // number of segments to chop the line into
                var count = GeoCoord.FromCoordinate(vertA)
                    .LineHexEstimate(GeoCoord.FromCoordinate(vertB), resolution);

                for (int j = 1; j < count; j += 1) {
                    // interpolate line
                    var interpolated = LinearLocation.PointAlongSegmentByFraction(vertA, vertB, (double)j / count);
                    var index = H3Index.FromCoordinate(interpolated, resolution);
                    indicies.Add(index);
                }
            }

            return indicies;
        }

        /// <summary>
        /// Determines whether or not the geometry is flagged as transmeridian;
        /// that is, has an arc > 180 deg lon.
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static bool IsTransMeridian(this Geometry geometry) {
            if (geometry.IsEmpty) return false;
            var coords = geometry.Envelope.Coordinates;
            return Math.Abs(coords[0].X - coords[2].X) > 180.0;
        }

        /// <summary>
        /// Attempts to split a polygon that spans the antemeridian into
        /// a multipolygon by clipping coordinates on either side of it and
        /// then unioning them back together again.
        /// </summary>
        /// <param name="originalGeometry"></param>
        /// <returns></returns>
        private static Geometry SplitGeometry(Geometry originalGeometry) {
            var left = originalGeometry.Copy();
            left.Apply(_negativeLonFilter);
            var right = originalGeometry.Copy();
            right.Apply(_positiveLonFilter);

            var geometry = left.Union(right);
            return geometry.IsEmpty ? originalGeometry : geometry;
        }

    }

}
