using System;
using System.Collections.Generic;
using System.Linq;
using H3.Model;
using static H3.Utils;

#nullable enable

namespace H3 {

	public static class H3IndexExtensions {

        /// <summary>
        /// Returns the hexagon index neighboring the origin, in the direction dir.
        ///
        /// Implementation note: The only reachable case where this returns 0 is if the
        /// origin is a pentagon and the translation is in the k direction. Thus,
        /// 0 can only be returned if origin is a pentagon.
        /// </summary>
        /// <param name="origin">Origin index</param>
        /// <param name="direction">Direction to move in</param>
        /// <param name="rotations">Number of CCW rotations to perform to reorient the
        /// translation vector. Will be modified to the new number of rotations to perform
        /// (such as when crossing a face edge.)</param>
        /// <returns>H3Index of the specified neighbor or H3_NULL if deleted k-subsequence
        /// distortion is encountered.</returns>
        public static H3Index NeighbourRotations(this H3Index origin, CellIndex direction, ref int rotations) {
            H3Index outIndex = new H3Index(origin);

            BaseCell? oldBaseCell = origin.BaseCell;
            if (oldBaseCell == null) throw new Exception("origin is not a valid base cell");

            CellIndex oldLeadingIndex = origin.LeadingNonZeroCellIndex;
            CellIndex curIndex = direction;
            for (int r = 0; r < rotations; r += 1) curIndex.RotateCounterClockwise();

            int newRotations = 0;

            // Adjust the indexing digits and, if needed, the base cell.
            int resolution = outIndex.Resolution - 1;
            while (true) {
                if (resolution == -1) {
                    outIndex.BaseCellNumber = LookupTables.Neighbours[origin.BaseCellNumber, (int)curIndex];
                    newRotations = LookupTables.NeighbourCounterClockwiseRotations[origin.BaseCellNumber, (int)curIndex];

                    if (outIndex.BaseCellNumber == LookupTables.INVALID_BASE_CELL) {
                        // Adjust for the deleted k vertex at the base cell level.
                        // This edge actually borders a different neighbor.
                        outIndex.BaseCellNumber = LookupTables.Neighbours[origin.BaseCellNumber, (int)CellIndex.IK];
                        newRotations = LookupTables.NeighbourCounterClockwiseRotations[origin.BaseCellNumber, (int)CellIndex.IK];

                        // perform the adjustment for the k-subsequence we're skipping
                        // over.
                        outIndex.RotateCounterClockwise();
                        rotations += 1;
                    }

                    break;
                } else {
                    CellIndex oldIndex = outIndex.GetCellIndexForResolution(resolution + 1);
                    CellIndex nextIndex;

                    // TODO are these swapped...?
                    if (IsResolutionClass3(resolution + 1)) {
                        outIndex.SetCellIndexForResolution(
                            resolution + 1,
                            LookupTables.NewDirectionClass2[(int)oldIndex, (int)curIndex]
                        );
                        nextIndex = LookupTables.NewAdjustmentClass2[(int)oldIndex, (int)curIndex];
                    } else {
                        outIndex.SetCellIndexForResolution(
                            resolution + 1,
                            LookupTables.NewDirectionClass3[(int)oldIndex, (int)curIndex]
                        );
                        nextIndex = LookupTables.NewAdjustmentClass3[(int)oldIndex, (int)curIndex];
                    }

                    if (nextIndex != CellIndex.Center) {
                        curIndex = nextIndex;
                        resolution--;
                    } else {
                        // No more adjustment to perform
                        break;
                    }
                }
            }

            BaseCell? newBaseCell = outIndex.BaseCell;
            // TODO exception instead...?  this doesn't match upstream behaviour
            if (newBaseCell == null) return H3Index.Invalid;

            if (newBaseCell.IsPentagon) {
                bool alreadyAdjustedKSubsequence = false;

                // force rotation out of missing k-axes sub-sequence
                if (outIndex.LeadingNonZeroCellIndex == CellIndex.K) {
                    if (oldBaseCell != newBaseCell) {
                        // in this case, we traversed into the deleted
                        // k subsequence of a pentagon base cell.
                        // We need to rotate out of that case depending
                        // on how we got here.
                        // check for a cw/ccw offset face; default is ccw
                        if (newBaseCell.FaceMatchesOffset(oldBaseCell.Home.Face)) {
                            outIndex.RotateClockwise();
                        } else {
                            outIndex.RotateCounterClockwise();
                        }

                        alreadyAdjustedKSubsequence = true;
                    } else {
                        // In this case, we traversed into the deleted
                        // k subsequence from within the same pentagon
                        // base cell.
                        if (oldLeadingIndex == CellIndex.Center) {
                            // Undefined: the k direction is deleted from here
                            return H3Index.Invalid;
                        } else if (oldLeadingIndex == CellIndex.JK) {
                            // Rotate out of the deleted k subsequence
                            // We also need an additional change to the direction we're
                            // moving in
                            outIndex.RotateCounterClockwise();
                            rotations += 1;
                        } else if (oldLeadingIndex == CellIndex.IK) {
                            // Rotate out of the deleted k subsequence
                            // We also need an additional change to the direction we're
                            // moving in
                            outIndex.RotateClockwise();
                            rotations += 5;
                        } else {
                            // should never happen
                            return H3Index.Invalid;
                        }
                    }
                }

                for (int i = 0; i < newRotations; i += 1) outIndex.RotatePentagonCounterClockwise();

                // Account for differing orientation of the base cells (this edge
                // might not follow properties of some other edges.)
                if (oldBaseCell != newBaseCell) {
                    if (newBaseCell.IsPolarPentagon) {
                        // 'polar' base cells behave differently because they have all
                        // i neighbors.
                        if (oldBaseCell.Cell != 118 && oldBaseCell.Cell != 8 && outIndex.LeadingNonZeroCellIndex != CellIndex.IK && !alreadyAdjustedKSubsequence) {
                            rotations += 1;
                        }
                    } else if (outIndex.LeadingNonZeroCellIndex == CellIndex.IK && !alreadyAdjustedKSubsequence) {
                        // account for distortion introduced to the 5 neighbor by the
                        // deleted k subsequence.
                        rotations += 1;
                    }
                }
            } else {
                for (int i = 0; i < newRotations; i += 1) outIndex.RotateCounterClockwise();
            }

            rotations = (rotations + newRotations) % 6;

            return outIndex;
        }

        /// <summary>
        /// Get the direction from the origin to a given neighbor. This is effectively
        /// the reverse operation for NeighborRotations. Returns CellIndex.Invalid if the
        /// cells are not neighbors.
        ///
        /// TODO: This is currently a brute-force algorithm, but as it's O(6) that's
        /// probably acceptable.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static CellIndex DirectionForNeighbour(this H3Index origin, H3Index destination) {
            bool isPentagon = origin.IsPentagon;

            for (CellIndex dir = isPentagon ? CellIndex.J : CellIndex.K; dir < CellIndex.Invalid; dir +=1) {
                int rotations = 0;
                H3Index neighbour = origin.NeighbourRotations(dir, ref rotations);
                if (neighbour == destination) return dir;
            }

            return CellIndex.Invalid;
        }

#if ZARDOZ
        public static H3Index[] Compact(H3Index[] h3Set) {
            if (h3Set.Length == 0) {
                return Array.Empty<H3Index>();
            }

            int resolution = h3Set[0].Resolution;
            if (!h3Set.Skip(1).All(h3 => h3.Resolution == resolution)) {
                throw new ArgumentException("all input indicies must be the same resolution");
            }

            H3Index[] remainingHexes = new H3Index[h3Set.Length];
            H3Index[] hashSetArray = new H3Index[h3Set.Length];
            Array.Copy(h3Set, 0, remainingHexes, 0, h3Set.Length);

            // no compaction possible; just return the whole input set
            if (resolution == 0) {
                return h3Set;
            }

            int numRemainingHexes = h3Set.Length;
            H3Index[] compactedSet = new H3Index[h3Set.Length];
            int compactedOffset = 0;

            while (numRemainingHexes > 0) {
                resolution = remainingHexes[0].Resolution;
                int parentRes = resolution - 1;
                for (int i = 0; i < numRemainingHexes; i += 1) {
                    H3Index currIndex = remainingHexes[i];
                    if (currIndex != 0) {
                        H3Index parent = currIndex.GetParentForResolution(parentRes);
                        int loc = (int)(parent % (ulong)numRemainingHexes);
                    }
                }
            }

        }

#endif
    }
}
