using System;
using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using H3.Model;
using static H3.Utils;
using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;

#nullable enable

namespace H3.Algorithms {

    /// <summary>
    /// Polyfill algorithms for H3Index.
    /// </summary>
    public static class Polyfill {
        /// <summary>
        /// Returns all of the H3 indexes that are contained within the provided
        /// Polygon at the specified resolution.  Supports Polygons with holes.
        /// </summary>
        /// <param name="polygon">Containment polygon</param>
        /// <param name="resolution">H3 resolution</param>
        /// <returns>Indicies where center point is contained within polygon</returns>
        public static IEnumerable<H3Index> Fill(this IPolygon polygon, int resolution) {
            bool isTransMeridian = polygon.IsTransMeridian();
            var testPoly = isTransMeridian ? ShiftPolygonMeridian(polygon) : polygon;

            HashSet<H3Index> searched = new();

            Stack<H3Index> toSearch = new(GetEdgeIndicies(testPoly, resolution));
            IndexedPointInAreaLocator locator = new(testPoly);

            while (toSearch.Count != 0) {
                var index = toSearch.Pop();

                if (index != H3Index.Invalid) {
                    foreach (var neighbour in GetKRingInPolygon(index, locator, isTransMeridian, searched)) {
                        yield return neighbour;
                        toSearch.Push(neighbour);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether or not the polygon is flagged as transmeridian;
        /// that is, has an arc > 180 deg lon.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static bool IsTransMeridian(this IPolygon polygon) {
            if (polygon.IsEmpty) return false;
            var coords = polygon.Envelope.Coordinates;
            return Math.Abs(coords[0].X - coords[2].X) > 180.0;
        }

        /// <summary>
        /// Shifts the coordinates for a transmeridian polygon such that it no
        /// longer spans the meridian (shifts all -'ve longitudes by 360 degrees)
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="geomFactory"></param>
        /// <returns></returns>
        public static IPolygon ShiftPolygonMeridian(this IPolygon polygon, GeometryFactory? geomFactory = null) {
            var gf = geomFactory ?? DefaultGeometryFactory;

            // transform coordinates for shell + holes
            var shell = new LinearRing(polygon.Shell.Coordinates.Select(ShiftMeridian).ToArray());
            var holes = polygon.Holes.Select(
                hole => new LinearRing(hole.Coordinates.Select(ShiftMeridian).ToArray())).ToArray();

            return gf.CreatePolygon(shell, holes);
        }

        /// <summary>
        /// Shift a coordinate by 360 degrees longitude.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static Coordinate ShiftMeridian(this Coordinate c) => new Coordinate(c.X < 0 ? c.X + 360 : c.X, c.Y);

        /// <summary>
        /// Gets all of the H3 indexes that define the boundary of the
        /// provided polygon.  This is used to seed the k ring search /
        /// point in polygon testing phase.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private static IEnumerable<H3Index> GetEdgeIndicies(IPolygon polygon, int resolution) {
            // TODO do some testing to see whether or not centroid or something else
            //      does the same trick here so we don't bother having to do all this work
            HashSet<H3Index> indicies = new();
            Coordinate[] coordinates = polygon.Coordinates;

            // trace the edge of the polygon; note this differs slightly from
            // upstream because NTS makes sure the polygons are already closed,
            // so we don't need to loop around to the first vertex ourselves
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
                    var interpolated = LinearLocation.PointAlongSegmentByFraction(vertA, vertB, j / count);
                    var index = H3Index.FromCoordinate(interpolated, resolution);
                    if (!indicies.Contains(index)) indicies.Add(index);
                }
            }

            return indicies;
        }

        /// <summary>
        /// Executes a k = 1 neighbour search for the provided H3 index, returning
        /// any neighbours that have center points contained within the provided
        /// polygon and that are not already present within the provided search
        /// history hashset.
        /// </summary>
        /// <param name="index">H3 index to get neighbours for</param>
        /// <param name="locator">IndexedPointInAreaLocator to use for point-in-poly
        /// checks</param>
        /// <param name="needsShift">Whether or not the polygon spans the
        /// meridian (> 180 deg longitudal arc) and requires we normalize/shift
        /// index coordinates by 360 degrees longitude when calculating the index
        /// center point.</param>
        /// <param name="searched">Hashset of previously searched indicies; will
        /// be updated to include any newly discovered neighbours automatically.
        /// </param>
        /// <returns>Neighbouring H3 indicies who's center points are contained
        /// within the provided polygon</returns>
        private static IEnumerable<H3Index> GetKRingInPolygon(H3Index index, IndexedPointInAreaLocator locator, bool needsShift, HashSet<H3Index> searched) =>
            index.GetKRingFast(1)
                .Where(cell => {
                    if (searched.Contains(cell.Index)) {
                        return false;
                    }
                    searched.Add(cell.Index);
                    var coord = cell.Index.ToPoint().Coordinate;
                    var location = locator.Locate(needsShift ? coord.ShiftMeridian() : coord);
                    return location == Location.Interior;
                })
                .Select(cell => cell.Index);

    }

}
