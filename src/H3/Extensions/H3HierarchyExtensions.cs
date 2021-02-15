using System;
using System.Collections.Generic;
using H3.Model;
using H3.Algorithms;
using static H3.Constants;
using static H3.Utils;
using System.Linq;

#nullable enable

namespace H3.Extensions {

    /// <summary>
    /// Extends the H3Index class with support for bitwise hierarchial queries.
    /// </summary>
    public static class H3HierarchyExtensions {

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
        public static H3Index GetDirectNeighbour(this H3Index origin, Direction direction, ref int rotations) {
            H3Index outIndex = new H3Index(origin);

            BaseCell? oldBaseCell = origin.BaseCell;
            if (oldBaseCell == null) throw new Exception("origin is not a valid base cell");

            Direction oldLeadingIndex = origin.LeadingNonZeroDirection;
            Direction curIndex = direction;
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
                        outIndex.BaseCellNumber = LookupTables.Neighbours[origin.BaseCellNumber, (int)Direction.IK];
                        newRotations = LookupTables.NeighbourCounterClockwiseRotations[origin.BaseCellNumber, (int)Direction.IK];

                        // perform the adjustment for the k-subsequence we're skipping
                        // over.
                        outIndex.RotateCounterClockwise();
                        rotations += 1;
                    }

                    break;
                } else {
                    Direction oldIndex = outIndex.GetDirectionForResolution(resolution + 1);
                    Direction nextIndex;

                    // TODO are these swapped...?
                    if (IsResolutionClass3(resolution + 1)) {
                        outIndex.SetDirectionForResolution(
                            resolution + 1,
                            LookupTables.NewDirectionClass2[(int)oldIndex, (int)curIndex]
                        );
                        nextIndex = LookupTables.NewAdjustmentClass2[(int)oldIndex, (int)curIndex];
                    } else {
                        outIndex.SetDirectionForResolution(
                            resolution + 1,
                            LookupTables.NewDirectionClass3[(int)oldIndex, (int)curIndex]
                        );
                        nextIndex = LookupTables.NewAdjustmentClass3[(int)oldIndex, (int)curIndex];
                    }

                    if (nextIndex != Direction.Center) {
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
                if (outIndex.LeadingNonZeroDirection == Direction.K) {
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
                        if (oldLeadingIndex == Direction.Center) {
                            // Undefined: the k direction is deleted from here
                            return H3Index.Invalid;
                        } else if (oldLeadingIndex == Direction.JK) {
                            // Rotate out of the deleted k subsequence
                            // We also need an additional change to the direction we're
                            // moving in
                            outIndex.RotateCounterClockwise();
                            rotations += 1;
                        } else if (oldLeadingIndex == Direction.IK) {
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
                        if (oldBaseCell.Cell != 118 && oldBaseCell.Cell != 8 && outIndex.LeadingNonZeroDirection != Direction.IK && !alreadyAdjustedKSubsequence) {
                            rotations += 1;
                        }
                    } else if (outIndex.LeadingNonZeroDirection == Direction.IK && !alreadyAdjustedKSubsequence) {
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
        /// the reverse operation for NeighborRotations. Returns Direction.Invalid if the
        /// cells are not neighbors.
        ///
        /// TODO: This is currently a brute-force algorithm, but as it's O(6) that's
        /// probably acceptable.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static Direction DirectionForNeighbour(this H3Index origin, H3Index destination) {
            bool isPentagon = origin.IsPentagon;

            for (Direction dir = isPentagon ? Direction.J : Direction.K; dir < Direction.Invalid; dir += 1) {
                int rotations = 0;
                H3Index neighbour = origin.GetDirectNeighbour(dir, ref rotations);
                if (neighbour == destination) return dir;
            }

            return Direction.Invalid;
        }

        /// <summary>
        /// Returns whether or not the provided H3Indexes are neighbors.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static bool IsNeighbour(this H3Index origin, H3Index destination) {
            // must be in hexagon mode
            if (origin.Mode != Mode.Hexagon || destination.Mode != Mode.Hexagon) {
                return false;
            }

            // can't be equal
            if (origin == destination) {
                return false;
            }

            // must be the same resolution
            int resolution = origin.Resolution;
            if (resolution != destination.Resolution) {
                return false;
            }

            // H3 Indexes that share the same parent are very likely to be neighbors
            // Child 0 is neighbor with all of its parent's 'offspring', the other
            // children are neighbors with 3 of the 7 children. So a simple comparison
            // of origin and destination parents and then a lookup table of the children
            // is a super-cheap way to possibly determine they are neighbors.
            int parentRes = resolution - 1;
            if (parentRes > 0 && origin.GetParentForResolution(parentRes) == destination.GetParentForResolution(parentRes)) {
                Direction originResDigit = origin.Direction;
                Direction destResDigit = destination.Direction;

                if (originResDigit == Direction.Center || destResDigit == Direction.Center) {
                    return true;
                }

                if (originResDigit.RotateClockwise() == destResDigit || originResDigit.RotateCounterClockwise() == destResDigit) {
                    return true;
                }
            }

            // Otherwise, we have to determine the neighbor relationship the "hard" way.
            return origin.GetKRingFast(1).Any(cell => cell.Index == destination);
        }

        /// <summary>
        /// Produces the parent index for a given H3 index at the specified
        /// resolution.
        /// </summary>
        /// <param name="origin">origin index</param>
        /// <param name="parentResolution">parent resolution, must be &gt;= 0 &lt; resolution</param>
        /// <returns>H3Index of parent</returns>
        public static H3Index GetParentForResolution(this H3Index origin, int parentResolution) {
            int resolution = origin.Resolution;

            // ask for an invalid resolution or resolution greater than ours?
            if (parentResolution < 0 || parentResolution > MAX_H3_RES || parentResolution > resolution) return H3Index.Invalid;

            // if its the same resolution, then we are our father.  err. yeah.
            if (resolution == parentResolution) return origin;

            // return the parent index
            H3Index parentIndex = new H3Index(origin) {
                Resolution = parentResolution
            };

            for (int r = parentResolution + 1; r <= resolution; r += 1)
                parentIndex.SetDirectionForResolution(r, Direction.Invalid);

            return parentIndex;
        }

        /// <summary>
        /// Returns the immediate child index based on the specified cell number.
        /// Bit operations only, could generate invalid indexes if not careful
        /// (deleted cell under a pentagon).
        /// </summary>
        /// <param name="origin">origin index</param>
        /// <param name="direction">direction to travel</param>
        /// <returns></returns>
        public static H3Index GetDirectChild(this H3Index origin, Direction direction) => new H3Index(origin) {
            Resolution = origin.Resolution + 1,
            Direction = direction
        };

        /// <summary>
        /// Produces the center child index for a given H3 index at the specified
        /// resolution.
        /// </summary>
        /// <param name="origin">origin index to find center of</param>
        /// <param name="childResolution">the resolution to switch to, must be &gt; resolution &lt;= MAX_H3_RES</param>
        /// <returns>H3Index of the center child, or H3Index.Invalid if you actually asked for a parent</returns>
        public static H3Index GetChildCenterForResolution(this H3Index origin, int childResolution) {
            int resolution = origin.Resolution;
            if (!IsValidChildResolution(resolution, childResolution)) return H3Index.Invalid;
            if (resolution == childResolution) return origin;

            H3Index childIndex = new H3Index(origin) {
                Resolution = childResolution
            };

            for (int r = resolution + 1; r <= childResolution; r += 1) {
                childIndex.SetDirectionForResolution(r, Direction.Center);
            }

            return childIndex;
        }

        /// <summary>
        /// Returns the maximum number of children possible for a given child resolution.
        /// </summary>
        /// <param name="origin">index to find children for</param>
        /// <param name="childResolution">resolution of child level</param>
        /// <returns></returns>
        public static long GetMaxChildrenSizeForResolution(this H3Index origin, int childResolution) {
            int parentResolution = origin.Resolution;
            if (!IsValidChildResolution(parentResolution, childResolution)) return 0;
            // TODO this is changing upstream to be pentago aware; port changes assuming we
            //      need this method at all.  @see https://github.com/uber/h3/issues/412
            return IPow(7, childResolution - parentResolution);
        }

        /// <summary>
        /// Takes the given hexagon id and generates all of the children at the specified
        /// resolution.
        /// </summary>
        /// <param name="origin">index to find children for</param>
        /// <param name="childResolution">resolution of child level</param>
        /// <returns></returns>
        public static IEnumerable<H3Index> GetChildrenAtResolution(this H3Index origin, int childResolution) {
            int resolution = origin.Resolution;

            if (!IsValidChildResolution(resolution, childResolution)) {
                yield break;
            }

            if (resolution == childResolution) {
                yield return origin;
            } else {
                bool pentagon = origin.IsPentagon;
                for (Direction i = 0; i < Direction.Invalid; i += 1) {
                    if (pentagon && i == Direction.K) continue;
                    foreach (var child in origin.GetDirectChild(i).GetChildrenAtResolution(childResolution))
                        yield return child;
                }
            }
        }

    }
}
