using System;
using System.Collections.Generic;
using System.Linq;
using H3.Extensions;
using H3.Model;

#nullable enable

namespace H3.Algorithms {

    /// <summary>
    /// Holder for indexes produced from the k ring functions.
    /// </summary>
    public record RingCell {
        /// <summary>
        /// H3 index
        /// </summary>
        public H3Index Index { get; init; } = H3Index.Invalid;

        /// <summary>
        /// k cell distance from the origin (ring level)
        /// </summary>
        public int Distance { get; init; }
    }

    /// <summary>
    /// Indicates that k-ring traversal failed due to the ring starting on
    /// a pentagon or due to encountering indexes within the pentagon distortion
    /// area.
    /// </summary>
    public class HexRingPentagonException : Exception { }

    /// <summary>
    /// Indicates that k-ring traversal failed due to the ring encountering
    /// an index with deleted k-subsequence distortion.
    /// </summary>
    public class HexRingKSequenceException : Exception { }

    /// <summary>
    /// Extends the H3Index class with support for kRing and hex ring queries.
    /// </summary>
    public static class Rings {

        /// <summary>
        /// Returns the "hollow" ring of cells at exactly grid distance k from
        /// the origin cell. In particular, k=0 returns just the origin cell.
        ///
        /// An exception may be thrown in some cases, for example if a pentagon is
        /// encountered.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static IEnumerable<H3Index> GetHexRing(this H3Index origin, int k) {
            // Identity short-circuit; return origin if k == 0
            if (k == 0) {
                yield return origin;
                if (origin.IsPentagon) {
                    throw new HexRingPentagonException();
                }
                yield break;
            }

            H3Index index = origin;

            // break out to the requested ring
            var rotations = 0;
            for (var ring = 0; ring < k; ring +=1 ) {
                (index, rotations) = index.GetDirectNeighbour(LookupTables.NextRingDirection, rotations);
                if (index == H3Index.Invalid) throw new HexRingKSequenceException();
                if (index.IsPentagon) throw new HexRingPentagonException();
            }

            H3Index lastIndex = new(index);
            yield return index;

            for (var direction = 0; direction < 6; direction += 1) {
                for (var pos = 0; pos < k; pos += 1) {
                    (index, rotations) = index.GetDirectNeighbour(LookupTables.CounterClockwiseDirections[direction], rotations);
                    if (index == H3Index.Invalid) throw new HexRingKSequenceException();

                    // Skip the very last index, it was already added. We do
                    // however need to traverse to it because of the pentagonal
                    // distortion check, below.
                    if (pos != k - 1 || direction != 5) {
                        yield return index;
                        if (index.IsPentagon) throw new HexRingPentagonException();
                    }
                }
            }

            if (lastIndex != index) throw new HexRingPentagonException();
        }

        /// <summary>
        /// Produce cells from the given origin cell within distance k.  This first
        /// attempts to use the GetKRingFast method, and falls back to GetKRingSlow if
        /// the fast method fails (e.g. pentagonal distortion).
        ///
        /// k-ring 0 is defined as the origin cell, k-ring 1 is defined as k-ring 0 and
        /// all neighboring cells, and so on.
        ///
        /// Results are provided in no particular order.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static IEnumerable<RingCell> GetKRing(this H3Index origin, int k) {
            try {
                return origin.GetKRingFast(k).ToList();
            } catch {
                return origin.GetKRingSlow(k);
            }
        }

        /// <summary>
        /// Iteratively produces indexes within k cell distance of the origin index.  This
        /// is a higher-accuracy but slower version of GetKRingFast.
        ///
        /// k-ring 0 is defined as the origin index, k-ring 1 is defined as k-ring 0 and
        /// all neighboring indexes, and so on.
        /// </summary>
        /// <param name="origin">Origin location</param>
        /// <param name="k">k >= 0</param>
        /// <returns>all neighbours within k cell distance</returns>
        public static IEnumerable<RingCell> GetKRingSlow(this H3Index origin, int k) {
            // if not a valid index then nothing to do
            if (origin == H3Index.Invalid) yield break;

            // since k >= 0, start with origin
            Queue<RingCell> queue = new();
            Dictionary<ulong, int> searched = new();
            queue.Enqueue(new RingCell { Index = origin, Distance = 0 });

            while (queue.Count != 0) {
                var cell = queue.Dequeue();
                yield return cell;

                var nextK = cell.Distance + 1;
                if (nextK > k)
                    continue;

                foreach (var neighbour in cell.Index.GetNeighbours()) {
                    if (neighbour == origin || searched.TryGetValue(neighbour, out var previousK) && previousK <= nextK) {
                        continue;
                    }
                    searched[neighbour] = nextK;
                    queue.Enqueue(new RingCell { Index = neighbour, Distance = nextK });
                }
            }
        }

        /// <summary>
        /// Produces indexes within k cell distance of the origin index.  This is a
        /// lower-accuracy but faster version of GetKRingSlow.
        ///
        /// k-ring 0 is defined as the origin index, k-ring 1 is defined as k-ring 0 and
        /// all neighboring indexes, and so on.
        ///
        /// Output behavior is undefined when one of the indexes returned by this
        /// function is a pentagon or is in the pentagon distortion area.
        /// </summary>
        /// <param name="origin">Origin location</param>
        /// <param name="k">k >= 0</param>
        /// <returns>Enumerable set of RingCell, or an exception if a traversal error is
        /// encountered (eg pentagon)</returns>
        public static IEnumerable<RingCell> GetKRingFast(this H3Index origin, int k) {
            H3Index index = origin;

            // k must be >= 0, so origin is always needed
            yield return new RingCell { Index = index, Distance = 0 };

            // Pentagon was encountered; bail out as user doesn't want this.
            if (index.IsPentagon) throw new HexRingPentagonException();

            // short circuit; k = 0 means we just want the origin (strange, but you get what you ask for)
            if (k == 0) yield break;

            // 0 < ring <= k, current ring
            var ring = 1;

            // 0 <= direction < 6, current side of the ring
            var direction = 0;

            // 0 <= i < ring, current position on the side of the ring
            var i = 0;

            // Number of 60 degree ccw rotations to perform on the direction (based on
            // which faces have been crossed.)
            var rotations = 0;

            while (ring <= k) {
                if (direction == 0 && i == 0) {
                    // Not putting in the output set as it will be done later, at
                    // the end of this ring.
                    (index, rotations) = index.GetDirectNeighbour(LookupTables.NextRingDirection, rotations);
                    if (index == H3Index.Invalid) {
                        // Should not be possible because `origin` would have to be a pentagon
                        throw new HexRingKSequenceException();
                    }

                    if (index.IsPentagon) {
                        // Pentagon was encountered; bail out as user doesn't want this.
                        throw new HexRingPentagonException();
                    }
                }

                (index, rotations) = index.GetDirectNeighbour(LookupTables.CounterClockwiseDirections[direction], rotations);
                if (index == H3Index.Invalid) {
                    // Should not be possible because `origin` would have to be a pentagon
                    throw new HexRingKSequenceException();
                }

                yield return new RingCell { Index = index, Distance = ring };
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
                    throw new HexRingPentagonException();
                }
            }
        }

    }

}
