using System;
using System.Collections.Generic;
using static H3.Utils;

namespace H3.Extensions {

    /// <summary>
    /// Provides extension methods that operate on arrays (sets) of H3Index.
    /// </summary>
    public static class H3ArrayExtensions {

        /// <summary>
        /// Takes a set of hexagons all at the same resolution and compresses
        /// them by pruning full child branches to the parent level. This is also
        /// done for all parents recursively to get the minimum number of hex
        /// addresses that perfectly cover the defined space.
        /// </summary>
        /// <param name="indexes">set of hexagons</param>
        /// <returns>array of compressed hexagons</returns>
        public static H3Index[] Compact(this H3Index[] indexes) {
            // TODO this algorithm presently requires allocating 3x the size of the
            // input array in order to produce the compressed set of indexes.  Can't
            // we do better?
            if (!indexes.AreOfSameResolution()) {
                throw new ArgumentException("all indexes must be the same resolution");
            }

            int resolution = indexes[0].Resolution;

            // cant compress beyond res0
            if (resolution == 0) return indexes;

            // worst-case scenario is we don't compress anything
            H3Index[] compactedSet = new H3Index[indexes.Length];

            H3Index[] remainingSet = new H3Index[indexes.Length];
            Array.Copy(indexes, remainingSet, indexes.Length);

            H3Index[] hashSet = new H3Index[indexes.Length];
            Array.Fill(hashSet, (H3Index)0);

            int compactedSetOffset = 0;
            int numRemaining = indexes.Length;
            while (numRemaining > 0) {
                resolution = remainingSet[0].Resolution;
                int parentResolution = resolution - 1;

                // Put the parents of the hexagons into the temp array
                // via a hashing mechanism, and use the reserved bits
                // to track how many times a parent is duplicated
                for (int i = 0; i < numRemaining; i += 1) {
                    H3Index curIndex = remainingSet[i];

                    if (curIndex != H3Index.Invalid) {
                        H3Index parent = curIndex.GetParentForResolution(parentResolution);

                        // Modulus hash the parent into the temp array
                        int loc = (int)(parent % (ulong)numRemaining);
                        int loopCount = 0;
                        while (hashSet[loc] != H3Index.Invalid) {
                            if (loopCount > numRemaining) {
                                throw new Exception("compaction loop exceeded");
                            }

                            H3Index tempIndex = hashSet[loc] & H3Index.H3_RESERVED_MASK_NEGATIVE;
                            if (tempIndex == parent) {
                                int count = hashSet[loc].ReservedBits + 1;
                                int limitCount = 7;

                                if (((H3Index)(tempIndex & H3Index.H3_RESERVED_MASK_NEGATIVE)).IsPentagon) {
                                    limitCount--;
                                }

                                // One is added to count for this check to match one
                                // being added to count later in this function when
                                // checking for all children being present.
                                if (count + 1 > limitCount) {
                                    // only possible on duplicate input
                                    throw new Exception("input contains duplicate index values");
                                }

                                parent.ReservedBits = count;
                                hashSet[loc] = 0;
                            } else {
                                loc = (loc + 1) & numRemaining;
                            }

                            loopCount++;
                        }

                        hashSet[loc] = parent;
                    }
                }

                // Determine which parent hexagons have a complete set
                // of children and put them in the compactableHexes array
                int compactableCount = 0;
                int maxCompactableCount = numRemaining / 6;  // Somehow all pentagons; conservative
                H3Index[] compactableSet = new H3Index[maxCompactableCount];

                if (maxCompactableCount == 0) {
                    Array.Copy(remainingSet, 0, compactedSet, compactedSetOffset, numRemaining);
                    break;
                }

                for (int i = 0; i < numRemaining; i += 1) {
                    if (hashSet[i] == H3Index.Invalid) continue;

                    int count = hashSet[i].ReservedBits + 1;

                    // Include the deleted direction for pentagons as implicitly "there"
                    if (((H3Index)(hashSet[i] & H3Index.H3_RESERVED_MASK_NEGATIVE)).IsPentagon) {
                        // We need this later on, no need to recalculate
                        hashSet[i].ReservedBits = count;
                        count++;
                    }

                    if (count == 7) {
                        // Bingo! Full set!
                        compactableSet[compactableCount] = hashSet[i] & H3Index.H3_RESERVED_MASK_NEGATIVE;
                        compactableCount++;
                    }
                }

                // Uncompactable hexes are immediately copied into the
                // output compactedSetOffset
                int uncompactableCount = 0;
                for (int i = 0; i < numRemaining; i += 1) {
                    H3Index curIndex = remainingSet[i];
                    if (curIndex != H3Index.Invalid) {
                        H3Index parent = curIndex.GetParentForResolution(parentResolution);

                        // Modulus hash the parent into the temp array
                        // to determine if this index was included in
                        // the compactableHexes array
                        int loc = (int)(parent % (ulong)numRemaining);
                        int loopCount = 0;
                        bool isUncompactable = true;

                        while (hashSet[loc] != parent) {
                            if (loopCount > numRemaining) {
                                throw new Exception("compaction loop exceeded");
                            }

                            H3Index tempIndex = hashSet[loc] & H3Index.H3_RESERVED_MASK_NEGATIVE;
                            if (tempIndex == parent) {
                                int count = hashSet[loc].ReservedBits + 1;
                                if (count == 7) isUncompactable = false;
                                break;
                            } else {
                                loc = (loc + 1) % numRemaining;
                            }

                            loopCount++;
                        }

                        if (isUncompactable) {
                            compactedSet[compactedSetOffset + uncompactableCount] = remainingSet[i];
                            uncompactableCount++;
                        }
                    }
                }

                // set up for next loop
                Array.Fill(hashSet, (H3Index)0);
                compactedSetOffset += uncompactableCount;
                Array.Copy(compactableSet, 0, remainingSet, 0, compactableCount);
                numRemaining = compactableCount;
            }

            Array.Resize(ref compactedSet, compactedSetOffset + 1);
            return compactedSet;
        }

        /// <summary>
        /// Takes a compressed set of hexagons and expands back to the original
        /// set of hexagons at a specific resoution.
        /// </summary>
        /// <param name="indexes">set of hexagons</param>
        /// <param name="resolution">resolution to decompress to</param>
        /// <returns>original set of hexagons.  Will throw an ArgumentException
        /// if any hexagon in the set is smaller than the output resolution
        /// </returns>
        public static H3Index[] UncompactToResolution(this H3Index[] indexes, int resolution) {
            List<H3Index> result = new();

            for (int i = 0; i < indexes.Length; i += 1) {
                if (indexes[i] == H3Index.Invalid) continue;

                int currentResolution = indexes[i].Resolution;
                if (!IsValidChildResolution(currentResolution, resolution)) {
                    throw new ArgumentException("set contains hexagon smaller than target resolution");
                }

                if (currentResolution == resolution) {
                    result.Add(indexes[i]);
                } else {
                    result.AddRange(indexes[i].GetChildrenForResolution(resolution));
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Takes a compacted set of hexagons are provides an upper-bound estimate
        /// on the number of uncompacted hexagons at the specified resolution.
        /// </summary>
        /// <param name="indexes">set of hexagons</param>
        /// <param name="resolution">resolution to uncompress to</param>
        /// <returns>the upper-bound estimate on number of uncompacted hexagons,
        /// or -1 if any hexagon in the set is smaller than the output resolution
        /// </returns>
        public static long MaximumUncompactedSizeForResolution(this H3Index[] indexes, int resolution) {
            long maxNumber = 0;
            for (int i = 0; i < indexes.Length; i += 1) {
                if (indexes[0] == H3Index.Invalid) continue;

                int currentResolution = indexes[i].Resolution;

                // non-sensical; abort
                if (!IsValidChildResolution(currentResolution, resolution)) return -1;

                if (currentResolution == resolution) {
                    maxNumber += 1;
                } else {
                    // bigger hexagon to reduce in size
                    maxNumber += indexes[i].GetMaxChildrenSizeForResolution(resolution);
                }
            }

            return maxNumber;
        }

        /// <summary>
        /// Determines whether or not all H3Index entries within the array are
        /// of the same resolution.
        /// </summary>
        /// <param name="indexes">set of hexagons</param>
        /// <returns>true if all hexagons are of the same resolution, false if
        /// not.
        /// </returns>
        public static bool AreOfSameResolution(this H3Index[] indexes) {
            if (indexes.Length <= 1) return true;
            int resolution = indexes[0].Resolution;
            for (int i = 1; i < indexes.Length; i += 1) {
                if (indexes[i].Resolution != resolution) return false;
            }

            return true;
        }

    }
}
