using System.Collections.Generic;
using H3.Model;

namespace H3.Algorithms {

    public enum HexRangeResult {
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
        /// GetHexRangeDistances produces indexes within k cell distance of the origin index.
        /// Output behavior is undefined when one of the indexes returned by this
        /// function is a pentagon or is in the pentagon distortion area.
        ///
        /// k-ring 0 is defined as the origin index, k-ring 1 is defined as k-ring 0 and
        /// all neighboring indexes, and so on.
        ///
        /// Output is placed in the provided enumerator in order of increasing distance from
        /// the origin.
        /// </summary>
        /// <param name="origin">Origin location</param>
        /// <param name="k">k >= 0</param>
        /// <param name="distances">out parameter for resultant distances</param>
        /// <returns>HexRangeResult</returns>
        public static HexRangeResult GetHexRangeDistances(this H3Index origin, int k, out List<HexRangeDistance> distances) {
            // TODO should this be an actual generator/iterator?
            H3Index index = new H3Index(origin);

            // k must be >= 0, so origin is always needed
            distances = new List<HexRangeDistance>() { new HexRangeDistance { Index = origin, Distance = 0 } };

            // Pentagon was encountered; bail out as user doesn't want this.
            if (origin.IsPentagon) return HexRangeResult.Pentagon;

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
                        return HexRangeResult.KSequence;
                    }

                    if (index.IsPentagon) {
                        // Pentagon was encountered; bail out as user doesn't want this.
                        return HexRangeResult.Pentagon;
                    }
                }

                index = index.NeighbourRotations(LookupTables.CounterClockwiseDirections[direction], ref rotations);
                if (index == H3Index.Invalid) {
                    // Should not be possible because `origin` would have to be a
                    // pentagon
                    return HexRangeResult.Pentagon;
                }

                distances.Add(new HexRangeDistance { Index = index, Distance = ring });
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
                    return HexRangeResult.Pentagon;
                }
            }

            return HexRangeResult.Success;
        }

    }

}
