using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using H3.Model;
using NetTopologySuite.LinearReferencing;

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
        public static IEnumerable<H3Index> FillPolygon(IPolygon polygon, int resolution) {
            HashSet<H3Index> searched = new();

            Stack<H3Index> toSearch = new(GetEdgeIndicies(polygon, resolution));

            while (toSearch.Count != 0) {
                var index = toSearch.Pop();

                if (index != H3Index.Invalid) {
                    foreach (var neighbour in GetKRingInPolygon(index, polygon, searched)) {
                        yield return neighbour;
                        toSearch.Push(neighbour);
                    }
                }
            }
        }

        /// <summary>
        /// Gets all of the H3 indexes that define the boundary of the
        /// provided polygon.  This is used to seed the k ring search /
        /// point in polygon testing phase.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private static IEnumerable<H3Index> GetEdgeIndicies(IPolygon polygon, int resolution) {
            HashSet<H3Index> indicies = new();
            Coordinate[] coordinates = polygon.Coordinates;

            // trace the edge of the polygon
            for (int c = 0; c < coordinates.Length; c += 1) {
                // from this coordinate to next/first
                var vertA = coordinates[c];
                var vertB = coordinates[c + 1 == coordinates.Length ? 0 : c + 1];

                // estimate number of indicies between points, use that as a
                // number of segments to chop the line into
                var count = GeoCoord.FromCoordinate(vertA)
                    .LineHexEstimate(GeoCoord.FromCoordinate(vertB), resolution);

                for (int j = 1; j <= count; j += 1) {
                    // interpolate line
                    var interpolated = LinearLocation.PointAlongSegmentByFraction(vertA, vertB, j / count);
                    var index = H3Index.FromCoordinate(interpolated, resolution);
                    if (!indicies.Contains(index)) indicies.Add(index);
                }
            }

            return indicies;
        }

        /// <summary>
        /// Squee!
        /// </summary>
        /// <param name="index"></param>
        /// <param name="polygon"></param>
        /// <param name="searched"></param>
        /// <returns></returns>
        private static IEnumerable<H3Index> GetKRingInPolygon(H3Index index, IPolygon polygon, HashSet<H3Index> searched) =>
            index.GetKRingFast(1)
                .Where(cell => {
                    if (searched.Contains(cell.Index)) {
                        return false;
                    }
                    searched.Add(cell.Index);
                    return polygon.Contains(cell.Index.ToPoint());
                })
                .Select(cell => cell.Index);

    }

}
