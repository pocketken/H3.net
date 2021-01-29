using System;
using System.Collections.Generic;
using H3.Model;

#nullable enable

namespace H3.Algorithms {

    public enum HexRingResult {
        Success = 0,
        Pentagon,
        KSequence
    }

    public record RingCell {
        public H3Index Index { get; init; } = H3Index.Invalid;
        public int Distance { get; init; }
    }

    public static class Rings {

        /// <summary>
        /// Maximum number of cells that result from the kRing algorithm with the given
        /// k. Formula source and proof: https://oeis.org/A003215
        /// </summary>
        /// <param name="k">k value, k >= 0</param>
        /// <returns></returns>
        public static int MaxKRingSize(int k) => 3 * k * (k + 1) + 1;

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
        public static (HexRingResult, IEnumerable<H3Index>) GetHexRange(this H3Index origin, int k) {
            List<H3Index> indicies = new();
            var result = ForEachHexRange(origin, k, (index, _, __) => indicies.Add(index));
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
        public static (HexRingResult, H3Index[]) GetHexRanges(this H3Index[] origins, int k) {
            // TODO while this mimics the original API, is this really how we want to do it?
            long segmentSize = MaxKRingSize(k);
            H3Index[] indicies = new H3Index[segmentSize * origins.Length];

            for (int i = 0; i < origins.Length; i+= 1) {
                var ringResult = ForEachHexRange(origins[i], k, (index, _, j) => {
                    indicies[(i * segmentSize) + j] = index;
                });
                if (ringResult != HexRingResult.Success) return (ringResult, indicies);
            }

            return (HexRingResult.Success, indicies);
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
        public static (HexRingResult, IEnumerable<RingCell>) GetHexRangeDistances(this H3Index origin, int k) {
            List<RingCell> distances = new();
            var result = ForEachHexRange(origin, k, (index, distance, _) => distances.Add(new RingCell { Index = index, Distance = distance }));
            return (result, distances);
        }

        /// <summary>
        /// Produce cells from the given origin cell within distance k.  This is a
        /// "higher-accuracy", but slower performing, version of GetHexRange.
        ///
        /// k-ring 0 is defined as the origin cell, k-ring 1 is defined as k-ring 0 and
        /// all neighboring cells, and so on.
        ///
        /// Results are provided in no particular order.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static IEnumerable<H3Index> GetKRing(this H3Index origin, int k) {
            HashSet<H3Index> indicies = new();
            ForEachKRingIndex(origin, k, (index, _) => {
                if (indicies.Contains(index)) return false;
                indicies.Add(index);
                return true;
            });
            return indicies;
        }

        /// <summary>
        /// Produce cells and their distances from the given origin cell within distance k.
        /// This is a "higher-accuracy", but slower performing, version of GetHexRangeDistances.
        ///
        /// k-ring 0 is defined as the origin cell, k-ring 1 is defined as k-ring 0 and
        /// all neighboring cells, and so on.
        ///
        /// Results are provided in no particular order.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static RingCell[] GetKRingDistances(this H3Index origin, int k) {
            // mimics the c library, more or less -- use array of max ring size as a hashset
            int maxSize = MaxKRingSize(k);
            RingCell[] indicies = new RingCell[maxSize];

            // iterate out to 0 <= k
            ForEachKRingIndex(origin, k, (index, distance) => {
                // calculate hash of index
                ulong off = index % (ulong)maxSize;

                // bump slot if we need to
                while (indicies[off] != null && indicies[off].Index != H3Index.Invalid && indicies[off].Index != origin) {
                    off = (off + 1) % (ulong)maxSize;
                }

                // We either got a free slot in the hash set or hit a duplicate
                // We might need to process the duplicate anyways because we got
                // here on a longer path before.
                if (indicies[off] != null && indicies[off].Index == origin && indicies[off].Distance <= distance) return false;

                indicies[off] = new RingCell { Index = index, Distance = distance };
                return true;
            });

            return indicies;
        }

        /// <summary>
        /// Recursively produce cells and their distances from the given origin cell within
        /// distance k.  This is a "higher-accuracy" version of ForEachHexRange, but may
        /// produce certain cells more than once as they may be seen from multiple paths/depths.
        ///
        /// Callback function should return true to keep recursing beyond the current cell,
        /// false to stop.
        ///
        /// k-ring 0 is defined as the origin cell, k-ring 1 is defined as k-ring 0 and
        /// all neighboring cells, and so on.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="k"></param>
        /// <param name="callback"></param>
        /// <param name="curK"></param>
        public static void ForEachKRingIndex(H3Index origin, int k, Func<H3Index, int, bool> callback, int curK = 0) {
            // if not a valid index or we've hit k, then stop
            if (origin == H3Index.Invalid) return;

            // if callback returns false, you shall not pass
            if (!callback(origin, curK)) return;

            // if we're at k, stop
            if (curK >= k) return;

            // rotate the index to get all neighbours and recurse
            H3Index index = new H3Index(origin);
            for (int i = 0; i < 6; i += 1) {
                int rotations = 0;
                ForEachKRingIndex(
                    index.NeighbourRotations(LookupTables.CounterClockwiseDirections[i], ref rotations),
                    k,
                    callback,
                    curK + 1
                );
            }
        }

        /// <summary>
        /// Guts of the hex range algorithm which non-recursively produces indexes within k
        /// cell distance of the origin index.  This is a lower-accuracy, but faster version of
        /// ForEachKRingIndex.
        ///
        /// k-ring 0 is defined as the origin index, k-ring 1 is defined as k-ring 0 and
        /// all neighboring indexes, and so on.
        ///
        /// Output behavior is undefined when one of the indexes returned by this
        /// function is a pentagon or is in the pentagon distortion area.
        /// </summary>
        /// <param name="origin">Origin location</param>
        /// <param name="k">k >= 0</param>
        /// <param name="callback">callback function which receives index, distance (ring)
        /// and total index count thus far for each result produced by the algorithm.</param>
        /// <returns>RingResult.Success on success</returns>
        public static HexRingResult ForEachHexRange(H3Index origin, int k, Action<H3Index, int, int> callback) {
            H3Index index = new H3Index(origin);

            // k must be >= 0, so origin is always needed
            callback(origin, 0, 0);

            // Pentagon was encountered; bail out as user doesn't want this.
            if (origin.IsPentagon) return HexRingResult.Pentagon;

            // 0 < ring <= k, current ring
            int ring = 1;

            // 0 <= direction < 6, current side of the ring
            int direction = 0;

            // 0 <= i < ring, current position on the side of the ring
            int i = 0;

            // Number of 60 degree ccw rotations to perform on the direction (based on
            // which faces have been crossed.)
            int rotations = 0;

            // total number of indicies generated
            int count = 1;

            while (ring <= k) {
                if (direction == 0 && i == 0) {
                    // Not putting in the output set as it will be done later, at
                    // the end of this ring.
                    index = index.NeighbourRotations(LookupTables.NextRingDirection, ref rotations);
                    if (index == H3Index.Invalid) {
                        // Should not be possible because `origin` would have to be a pentagon
                        return HexRingResult.KSequence;
                    }

                    if (index.IsPentagon) {
                        // Pentagon was encountered; bail out as user doesn't want this.
                        return HexRingResult.Pentagon;
                    }
                }

                index = index.NeighbourRotations(LookupTables.CounterClockwiseDirections[direction], ref rotations);
                if (index == H3Index.Invalid) {
                    // Should not be possible because `origin` would have to be a pentagon
                    return HexRingResult.Pentagon;
                }

                callback(index, ring, count++);
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
                    return HexRingResult.Pentagon;
                }
            }

            return HexRingResult.Success;
        }

    }

}
