using System;
using System.Collections.Generic;
using System.Linq;
using H3.Model;

namespace H3.Algorithms {

    public enum RingResult {
        Success = 0,
        Pentagon,
        KSequence
    }

    public record HexRangeDistance {
        public H3Index Index { get; init; }
        public int Distance { get; init; }
    }

    public static class Rings {

        /// <summary>
        /// Maximum number of cells that result from the kRing algorithm with the given
        /// k. Formula source and proof: https://oeis.org/A003215
        /// </summary>
        /// <param name="k">k value, k >= 0</param>
        /// <returns></returns>
        public static long MaxKRingSize(int k) => 3 * k * (k + 1) + 1;

        /// <summary>
        /// GetHexRange produces indexes within k distance of the origin index.
        /// Output behavior is undefined when one of the indexes returned by this
        /// function is a pentagon or is in the pentagon distortion area.
        ///
        /// k-ring 0 is defined as the origin index, k-ring 1 is defined as k-ring 0 and
        /// all neighboring indexes, and so on.
        ///
        /// Output is placed in the resulting enumerator in order of increasing distance from
        /// the origin.
        /// </summary>
        /// <param name="origin">Origin location</param>
        /// <param name="k">k >= 0</param>
        /// <returns>(RingResult.Success, IEnumerable) on success</returns>
        public static (RingResult, IEnumerable<H3Index>) GetHexRange(this H3Index origin, int k) {
            List<H3Index> indicies = new();
            var result = ForEachHexRange(origin, k, (index, _) => indicies.Add(index));
            return (result, indicies);
        }

        /// <summary>
        /// GetHexRanges takes an array of input hex IDs and a max k-ring and returns an
        /// array of hexagon IDs sorted first by the original hex IDs and then by the
        /// k-ring (0 to max), with no guaranteed sorting within each k-ring group.
        /// </summary>
        /// <param name="origins">Array of origin locations</param>
        /// <param name="k">k >= 0</param>
        /// <returns>(RingResult.Success, IEnumerable) on success</returns>
        public static (RingResult, H3Index[]) GetHexRanges(this H3Index[] origins, int k) {
            // TODO while this mimics the original API, is this really how we want to do it?
            long segmentSize = MaxKRingSize(k);
            H3Index[] indicies = new H3Index[segmentSize * origins.Length];

            for (int i = 0; i < origins.Length; i+= 1) {
                int j = 0;
                var ringResult = ForEachHexRange(origins[i], k, (index, _) => {
                    indicies[(i * segmentSize) + j++] = index;
                });
                if (ringResult != RingResult.Success) return (ringResult, indicies);
            }

            return (RingResult.Success, indicies);
        }

        /// <summary>
        /// GetHexRangeDistances produces indexes within k cell distance of the origin index.
        /// Output behavior is undefined when one of the indexes returned by this
        /// function is a pentagon or is in the pentagon distortion area.
        ///
        /// k-ring 0 is defined as the origin index, k-ring 1 is defined as k-ring 0 and
        /// all neighboring indexes, and so on.
        ///
        /// Output is placed in the resulting enumerator in order of increasing distance from
        /// the origin.
        /// </summary>
        /// <param name="origin">Origin location</param>
        /// <param name="k">k >= 0</param>
        /// <returns>(RingResult.Success, IEnumerable) on success</returns>
        public static (RingResult, IEnumerable<HexRangeDistance>) GetHexRangeDistances(this H3Index origin, int k) {
            List<HexRangeDistance> distances = new();
            var result = ForEachHexRange(origin, k, (index, distance) => distances.Add(new HexRangeDistance {
                Index = index,
                Distance = distance
            }));
            return (result, distances);
        }

        /// <summary>
        /// Guts of the hex range algorithm which produces indexes within k cell distance
        /// of the origin index.
        ///
        /// k-ring 0 is defined as the origin index, k-ring 1 is defined as k-ring 0 and
        /// all neighboring indexes, and so on.
        ///
        /// Output behavior is undefined when one of the indexes returned by this
        /// function is a pentagon or is in the pentagon distortion area.
        /// </summary>
        /// <param name="origin">Origin location</param>
        /// <param name="k">k >= 0</param>
        /// <param name="callback">callback function which receives index and distance for
        /// each result produced by the algorithm.</param>
        /// <returns>RingResult.Success on success</returns>
        public static RingResult ForEachHexRange(H3Index origin, int k, Action<H3Index, int> callback) {
            H3Index index = new H3Index(origin);

            callback(origin, 0);

            // Pentagon was encountered; bail out as user doesn't want this.
            if (origin.IsPentagon) return RingResult.Pentagon;

            // 0 < ring <= k, current ring
            int ring = 1;

            // 0 <= direction < 6, current side of the ring
            int direction = 0;

            // 0 <= i < ring, current position on the side of the ring
            int i = 0;

            // Number of 60 degree ccw rotations to perform on the direction (based on
            // which faces have been crossed.)
            int rotations = 0;

            while (ring <= k) {
                if (direction == 0 && i == 0) {
                    // Not putting in the output set as it will be done later, at
                    // the end of this ring.
                    index = index.NeighbourRotations(LookupTables.NextRingDirection, ref rotations);
                    if (index == H3Index.Invalid) {
                        // Should not be possible because `origin` would have to be a
                        // pentagon
                        return RingResult.KSequence;
                    }

                    if (index.IsPentagon) {
                        // Pentagon was encountered; bail out as user doesn't want this.
                        return RingResult.Pentagon;
                    }
                }

                index = index.NeighbourRotations(LookupTables.CounterClockwiseDirections[direction], ref rotations);
                if (index == H3Index.Invalid) {
                    // Should not be possible because `origin` would have to be a
                    // pentagon
                    return RingResult.Pentagon;
                }

                callback(index, ring);
                i += 1;

                // Check if end of this side of the k-ring
                if (i == ring) {
                    i = 0;
                    direction += 1;

                    // Check if end of this ring.
                    if (direction == 6) {
                        direction = 0;
                        ring += 1;
                    }
                }

                if (index.IsPentagon) {
                    return RingResult.Pentagon;
                }
            }

            return RingResult.Success;
        }

    }

}
