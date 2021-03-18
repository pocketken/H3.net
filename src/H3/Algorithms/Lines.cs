using System;
using System.Collections.Generic;
using System.Linq;
using H3.Extensions;
using H3.Model;

namespace H3.Algorithms {

    public static class Lines {

        /// <summary>
        /// Produces the grid distance between the two indexes.
        ///
        /// This function may fail to find the distance between two indexes, for
        /// example if they are very far apart. It may also fail when finding
        /// distances for indexes on opposite sides of a pentagon.
        /// </summary>
        /// <param name="origin">index to find distance from</param>
        /// <param name="destination">index to find distance to</param>
        /// <returns>grid distance in cells; -1 if could not be computed</returns>
        public static int DistanceTo(this H3Index origin, H3Index destination) {
            try {
                CoordIJK originIJK = LocalCoordIJK.ToLocalIJK(origin, origin);
                CoordIJK destinationIJK = LocalCoordIJK.ToLocalIJK(origin, destination);

                return originIJK.GetDistanceTo(destinationIJK);
            } catch {
                return -1;
            }
        }

        /// <summary>
        /// Given two H3 indexes, return the line of indexes between them (inclusive).
        /// </summary>
        /// <remarks>
        /// This function may fail to find the line between two indexes, for
        /// example if they are very far apart. It may also fail when finding
        /// distances for indexes on opposite sides of a pentagon.
        /// - The specific output of this function should not be considered stable
        ///   across library versions. The only guarantees the library provides are
        ///   that the line length will be `h3Distance(start, end) + 1` and that
        ///   every index in the line will be a neighbor of the preceding index.
        /// - Lines are drawn in grid space, and may not correspond exactly to either
        ///   Cartesian lines or great arcs.
        /// </remarks>
        /// <param name="origin">start index of the line</param>
        /// <param name="destination">end index of the line</param>
        /// <returns>all points from start to end, inclusive; empty if could not
        /// compute a line</returns>
        public static IEnumerable<H3Index> LineTo(this H3Index origin, H3Index destination) {
            int distance = origin.DistanceTo(destination);
            if (distance < 0) return Enumerable.Empty<H3Index>();

            // Get IJK coords for the start and end. We've already confirmed
            // that these can be calculated with the distance check above.
            // Convert IJK to cube coordinates suitable for linear interpolation
            CoordIJK startIjk = LocalCoordIJK.ToLocalIJK(origin, origin).Cube();
            CoordIJK endIjk = LocalCoordIJK.ToLocalIJK(origin, destination).Cube();

            double d = distance;
            double iStep = distance > 0 ? (endIjk.I - startIjk.I) / d : 0.0;
            double jStep = distance > 0 ? (endIjk.J - startIjk.J) / d : 0.0;
            double kStep = distance > 0 ? (endIjk.K - startIjk.K) / d : 0.0;

            double startI = startIjk.I;
            double startJ = startIjk.J;
            double startK = startIjk.K;

            return Enumerable.Range(0, distance + 1)
                .Select(n => {
                    var currentIjk = CoordIJK.CubeRound(
                        startI + iStep * n,
                        startJ + jStep * n,
                        startK + kStep * n
                    ).Uncube();
                    return LocalCoordIJK.ToH3Index(origin, currentIjk);
                });
        }

    }

}
