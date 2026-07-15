using System;
using System.Collections.Generic;
using H3.Model;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Extensions; 

/// <summary>
/// Extends the H3Index class with support for bitwise hierarchical queries.
/// </summary>
public static class H3HierarchyExtensions {

    /// <summary>
    /// Returns the cell index neighboring the origin, in the <see cref="Direction"/> dir.
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
    public static (H3Index, int) GetDirectNeighbour(this H3Index origin, Direction direction, int rotations = 0) {
        H3Index outIndex = new(origin);

        // Reorient the translation direction by the accumulated CCW rotation.  In
        // the overwhelmingly common rotations == 0 case — the entire interior of a
        // fast k-ring / k-disk traversal (no face crossing has occurred yet) and
        // every caller that takes the default — this reorientation is the identity,
        // so both the modulo reduction and the rotation-table lookup can be
        // skipped: RotateCounterClockwise by a zero rotation count always returns
        // the direction unchanged (CounterClockwise[(int)d * 6] == d for every
        // direction d).  For a non-zero rotation the modulo (which also bounds the
        // table index and guards the later `rotations += …` accumulations against
        // signed overflow) and the table lookup run exactly as before, so the
        // produced index and returned rotation count are bit-for-bit identical for
        // all inputs.
        Direction dir;
        if (rotations == 0) {
            dir = direction;
        } else {
            rotations %= 6;
            dir = direction.RotateCounterClockwise(rotations);
        }

        var oldBaseCellNumber = origin.BaseCellNumber;
        if (oldBaseCellNumber >= NUM_BASE_CELLS) throw new Exception("origin is not a valid base cell");

        var neighbourRotations = 0;

        // Adjust the indexing digits and, if needed, the base cell.
        var resolution = outIndex.Resolution - 1;
        while (true) {
            if (resolution == -1) {
                var newBaseCellNumber = BaseCells.GetNeighbouringCellNumber(oldBaseCellNumber, dir);
                neighbourRotations = BaseCells.GetNeighbourCounterClockwiseRotations(oldBaseCellNumber, dir);

                outIndex.BaseCellNumber = newBaseCellNumber;

                if (newBaseCellNumber == LookupTables.INVALID_BASE_CELL) {
                    // Adjust for the deleted k vertex at the base cell level.
                    // This edge actually borders a different neighbor.
                    outIndex.BaseCellNumber = BaseCells.GetNeighbouringCellNumber(oldBaseCellNumber, Direction.IK);
                    neighbourRotations = BaseCells.GetNeighbourCounterClockwiseRotations(oldBaseCellNumber, Direction.IK);

                    // perform the adjustment for the k-subsequence we're skipping
                    // over.
                    outIndex.RotateCounterClockwise();
                    rotations += 1;
                }

                break;
            }

            var nextResolution = resolution + 1;
            var oldDir = outIndex.GetDirectionForResolution(nextResolution);

            if (oldDir == Direction.Invalid) {
                // Only possible on invalid input
                return (H3Index.Invalid, rotations);
            }

            var packed = IsResolutionClass3(nextResolution)
                ? LookupTables.TraversalPackedClass2[(int)oldDir * 7 + (int)dir]
                : LookupTables.TraversalPackedClass3[(int)oldDir * 7 + (int)dir];
            outIndex.SetDirectionForResolution(nextResolution, (Direction)(packed & 7));
            var nextDir = (Direction)(packed >> 3);

            if (nextDir != Direction.Center) {
                dir = nextDir;
                resolution--;
            } else {
                // No more adjustment to perform
                break;
            }
        }

        // The digit walk crossed a base cell boundary iff it consumed every
        // resolution digit (resolution reached -1); otherwise it stopped on a
        // carry-free Center step with the base cell — and neighbourRotations
        // (still 0) — untouched.
        var crossedBaseCell = resolution == -1;

        // Common case (no base cell crossing, non-pentagon base cell): there is
        // no rotation fix-up to apply, so return immediately.  This skips the
        // base-cell re-extraction, the repeated pentagon test and — most
        // importantly — the large, non-inlined RotateCounterClockwise(0) call
        // the general tail would otherwise make.  rotations was already reduced
        // modulo 6 and is unchanged here (neighbourRotations == 0), so this is
        // bit-for-bit identical to running that tail.
        if (!crossedBaseCell && !BaseCells.IsPentagonCellNumber(oldBaseCellNumber)) {
            return (outIndex, rotations);
        }

        var newBaseCellNumber2 = crossedBaseCell ? outIndex.BaseCellNumber : oldBaseCellNumber;

        if (BaseCells.IsPentagonCellNumber(newBaseCellNumber2)) {
            var newBaseCell = BaseCells.Cells[newBaseCellNumber2];
            var alreadyAdjustedKSubsequence = false;

            // force rotation out of missing k-axes sub-sequence
            if (outIndex.LeadingNonZeroDirection == Direction.K) {
                if (oldBaseCellNumber != newBaseCellNumber2) {
                    // in this case, we traversed into the deleted
                    // k subsequence of a pentagon base cell.
                    // We need to rotate out of that case depending
                    // on how we got here.
                    // check for a cw/ccw offset face; default is ccw
                    if (newBaseCell.FaceMatchesOffset(BaseCells.Cells[oldBaseCellNumber].Home.Face)) {
                        outIndex.RotateClockwise();
                    } else {
                        outIndex.RotateCounterClockwise();
                    }

                    alreadyAdjustedKSubsequence = true;
                } else {
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (origin.LeadingNonZeroDirection) {
                        // In this case, we traversed into the deleted
                        // k subsequence from within the same pentagon
                        // base cell.
                        case Direction.Center:
                            // Undefined: the k direction is deleted from here
                            return (H3Index.Invalid, rotations);

                        case Direction.JK:
                            // Rotate out of the deleted k subsequence
                            // We also need an additional change to the direction we're
                            // moving in
                            outIndex.RotateCounterClockwise();
                            rotations += 1;
                            break;

                        case Direction.IK:
                            // Rotate out of the deleted k subsequence
                            // We also need an additional change to the direction we're
                            // moving in
                            outIndex.RotateClockwise();
                            rotations += 5;
                            break;

                        default:
                            // should never happen
                            return (H3Index.Invalid, rotations);
                    }

                }
            }

            for (var i = 0; i < neighbourRotations; i += 1) outIndex.RotatePentagonCounterClockwise();

            // Account for differing orientation of the base cells (this edge
            // might not follow properties of some other edges.)
            if (oldBaseCellNumber != newBaseCellNumber2) {
                if (newBaseCell.IsPolarPentagon) {
                    // 'polar' base cells behave differently because they have all
                    // i neighbors.
                    if (oldBaseCellNumber is not (118 or 8) && outIndex.LeadingNonZeroDirection != Direction.JK) {
                        rotations += 1;
                    }
                } else if (outIndex.LeadingNonZeroDirection == Direction.IK && !alreadyAdjustedKSubsequence) {
                    // account for distortion introduced to the 5 neighbor by the
                    // deleted k subsequence.
                    rotations += 1;
                }
            }
        } else {
            outIndex.RotateCounterClockwise(neighbourRotations);
        }

        rotations = (rotations + neighbourRotations) % 6;

        return (outIndex, rotations);
    }

    /// <summary>
    /// Rotations-free specialization of <see cref="GetDirectNeighbour"/> for callers
    /// that always pass <c>rotations = 0</c> and discard the returned rotation count
    /// (e.g. the polyfill flood fill and <c>GetDirectedEdgeDestination</c>).  The
    /// <c>rotations</c> accumulator in <see cref="GetDirectNeighbour"/> — apart from
    /// seeding <c>dir</c> from the (here zero) input — never feeds back into the
    /// produced index; it only forms the discarded output.  Dropping it removes a
    /// direction-rotation table load, several <c>rotations += …</c> updates, the
    /// trailing base-cell-orientation rotation block, a constant-modulo, and the
    /// <c>(H3Index,int)</c> tuple pack, while keeping every index-mutating rotation.
    /// The returned index is therefore bit-for-bit identical to
    /// <c>origin.GetDirectNeighbour(direction, 0).Item1</c>.
    /// </summary>
    /// <param name="origin">Origin index</param>
    /// <param name="direction">Direction to move in</param>
    /// <returns>H3Index of the specified neighbor or H3_NULL if deleted
    /// k-subsequence distortion is encountered.</returns>
    internal static H3Index GetDirectNeighbourWithoutRotations(this H3Index origin, Direction direction) {
        H3Index outIndex = new(origin);

        // rotations == 0, so dir == direction.RotateCounterClockwise(0) == direction
        var dir = direction;

        var oldBaseCellNumber = origin.BaseCellNumber;
        if (oldBaseCellNumber >= NUM_BASE_CELLS) throw new Exception("origin is not a valid base cell");

        var neighbourRotations = 0;

        // Adjust the indexing digits and, if needed, the base cell.
        var resolution = outIndex.Resolution - 1;
        while (true) {
            if (resolution == -1) {
                var newBaseCellNumber = BaseCells.GetNeighbouringCellNumber(oldBaseCellNumber, dir);
                neighbourRotations = BaseCells.GetNeighbourCounterClockwiseRotations(oldBaseCellNumber, dir);

                outIndex.BaseCellNumber = newBaseCellNumber;

                if (newBaseCellNumber == LookupTables.INVALID_BASE_CELL) {
                    // Adjust for the deleted k vertex at the base cell level.
                    outIndex.BaseCellNumber = BaseCells.GetNeighbouringCellNumber(oldBaseCellNumber, Direction.IK);
                    neighbourRotations = BaseCells.GetNeighbourCounterClockwiseRotations(oldBaseCellNumber, Direction.IK);

                    outIndex.RotateCounterClockwise();
                }

                break;
            }

            var nextResolution = resolution + 1;
            var oldDir = outIndex.GetDirectionForResolution(nextResolution);

            if (oldDir == Direction.Invalid) {
                // Only possible on invalid input
                return H3Index.Invalid;
            }

            var packed = IsResolutionClass3(nextResolution)
                ? LookupTables.TraversalPackedClass2[(int)oldDir * 7 + (int)dir]
                : LookupTables.TraversalPackedClass3[(int)oldDir * 7 + (int)dir];
            outIndex.SetDirectionForResolution(nextResolution, (Direction)(packed & 7));
            var nextDir = (Direction)(packed >> 3);

            if (nextDir != Direction.Center) {
                dir = nextDir;
                resolution--;
            } else {
                // No more adjustment to perform
                break;
            }
        }

        // The digit walk crossed a base cell boundary iff it consumed every
        // resolution digit (resolution reached -1); otherwise it stopped on a
        // carry-free Center step with the base cell — and neighbourRotations
        // (still 0) — untouched.
        var crossedBaseCell = resolution == -1;

        // Common case (no base cell crossing, non-pentagon base cell): there is
        // no rotation fix-up to apply, so return immediately.  This skips the
        // base-cell re-extraction, the repeated pentagon test and — most
        // importantly — the large, non-inlined RotateCounterClockwise(0) call
        // the general tail would otherwise make.  Bit-for-bit identical to it.
        if (!crossedBaseCell && !BaseCells.IsPentagonCellNumber(oldBaseCellNumber)) {
            return outIndex;
        }

        var newBaseCellNumber2 = crossedBaseCell ? outIndex.BaseCellNumber : oldBaseCellNumber;

        if (BaseCells.IsPentagonCellNumber(newBaseCellNumber2)) {
            var newBaseCell = BaseCells.Cells[newBaseCellNumber2];

            // force rotation out of missing k-axes sub-sequence
            if (outIndex.LeadingNonZeroDirection == Direction.K) {
                if (oldBaseCellNumber != newBaseCellNumber2) {
                    // in this case, we traversed into the deleted k subsequence of a
                    // pentagon base cell; rotate out of it depending on how we got here.
                    if (newBaseCell.FaceMatchesOffset(BaseCells.Cells[oldBaseCellNumber].Home.Face)) {
                        outIndex.RotateClockwise();
                    } else {
                        outIndex.RotateCounterClockwise();
                    }
                } else {
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (origin.LeadingNonZeroDirection) {
                        case Direction.Center:
                            // Undefined: the k direction is deleted from here
                            return H3Index.Invalid;

                        case Direction.JK:
                            outIndex.RotateCounterClockwise();
                            break;

                        case Direction.IK:
                            outIndex.RotateClockwise();
                            break;

                        default:
                            // should never happen
                            return H3Index.Invalid;
                    }
                }
            }

            for (var i = 0; i < neighbourRotations; i += 1) outIndex.RotatePentagonCounterClockwise();
        } else {
            outIndex.RotateCounterClockwise(neighbourRotations);
        }

        return outIndex;
    }

    /// <summary>
    /// Gets all of the neighbouring cells of <paramref name="origin"/>.  This is just a wrapper
    /// around calling <see cref="GetDirectNeighbour"/> for each <see cref="Direction"/> and
    /// filtering for <see cref="H3Index.Invalid"/>.
    /// </summary>
    /// <param name="origin">cell to get neighbours of</param>
    /// <returns></returns>
    public static IEnumerable<H3Index> GetNeighbours(this H3Index origin) {
        for (var direction = Direction.Center; direction < Direction.Invalid; direction += 1) {
            var (neighbour, _) = origin.GetDirectNeighbour(direction);
            if (neighbour == H3Index.Invalid) continue;
            yield return neighbour;
        }
    }

    /// <summary>
    /// Get the <see cref="Direction"/> from the origin to a given neighbor. This is effectively
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
        var isPentagon = origin.IsPentagon;

        for (var dir = isPentagon ? Direction.J : Direction.K; dir < Direction.Invalid; dir += 1) {
            var neighbour = origin.GetDirectNeighbour(dir).Item1;
            if (neighbour == destination) return dir;
        }

        return Direction.Invalid;
    }

    /// <summary>
    /// Returns whether or not the provided <see cref="H3Index"/> are neighbours.
    /// </summary>
    /// <param name="origin">Origin H3 index</param>
    /// <param name="destination">Destination H3 index</param>
    /// <returns>true if indexes are neighbours, false if not</returns>
    public static bool IsNeighbour(this H3Index origin, H3Index destination) {
        // must be in cell mode
        if (origin.Mode != Mode.Cell || destination.Mode != Mode.Cell) {
            return false;
        }

        // can't be equal
        if (origin == destination) {
            return false;
        }

        // must be the same resolution
        var resolution = origin.Resolution;
        if (resolution != destination.Resolution) {
            return false;
        }

        // H3 Indexes that share the same parent are very likely to be neighbors
        // Child 0 is neighbor with all of its parent's 'offspring', the other
        // children are neighbors with 3 of the 7 children. So a simple comparison
        // of origin and destination parents and then a lookup table of the children
        // is a super-cheap way to possibly determine they are neighbors.
        var parentRes = resolution - 1;
        if (parentRes > 0) {
            var originParentValue = origin.GetParentValueForResolution(parentRes);
            if (originParentValue == destination.GetParentValueForResolution(parentRes)) {
                var originResDigit = origin.Direction;
                var destResDigit = destination.Direction;

                if (originResDigit == Direction.Center || destResDigit == Direction.Center) {
                    return true;
                }

                if (originResDigit == Direction.Invalid) {
                    return false;
                }

                if ((originResDigit == Direction.K || destResDigit == Direction.K) &&
                    new H3Index(originParentValue).IsPentagon) {
                    // these are invalid cells within the deleted k subsequence of a
                    // pentagon; fail rather than incorrectly reporting neighbours.
                    // pentagon cells that are actually neighbours across the deleted
                    // subsequence will fail the optimized check below, but will be
                    // accepted by the neighbour check below that.
                    return false;
                }

                if (originResDigit.RotateClockwise() == destResDigit || originResDigit.RotateCounterClockwise() == destResDigit) {
                    return true;
                }
            }
        }

        // Otherwise, we have to determine the neighbor relationship the "hard" way.
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var neighbour in origin.GetNeighbours()) {
            if (neighbour == destination) return true;
        }

        return false;
    }

    /// <summary>
    /// Produces the parent index for a given <see cref="H3Index"/> at the specified
    /// resolution.
    /// </summary>
    /// <param name="origin">origin index</param>
    /// <param name="parentResolution">parent resolution, must be &gt;= 0 &lt; resolution</param>
    /// <returns>H3Index of parent</returns>
    public static H3Index GetParentForResolution(this H3Index origin, int parentResolution) {
        var resolution = origin.Resolution;

        // ask for an invalid resolution or resolution greater than ours?
        if (parentResolution is < 0 or > MAX_H3_RES || parentResolution > resolution) return H3Index.Invalid;

        // if its the same resolution, then we are our father.  err. yeah.
        if (resolution == parentResolution) return origin;

        // return the parent index
        return new H3Index(origin.GetParentValueForResolution(parentResolution));
    }

    /// <summary>
    /// Returns the immediate child <see cref="H3Index"/> in the specified <see cref="Direction"/>.
    /// Bit operations only, could generate invalid indexes if not careful
    /// (deleted cell under a pentagon).
    /// </summary>
    /// <param name="origin">origin index</param>
    /// <param name="direction">direction to travel</param>
    /// <returns></returns>
    public static H3Index GetDirectChild(this H3Index origin, Direction direction) => new(origin) {
        Resolution = origin.Resolution + 1,
        Direction = direction
    };

    /// <summary>
    /// Produces the center child index for a given <see cref="H3Index"/> at the specified
    /// resolution.
    /// </summary>
    /// <param name="origin">origin index to find center of</param>
    /// <param name="childResolution">the resolution to switch to, must be &gt; resolution &lt;= MAX_H3_RES</param>
    /// <returns><see cref="H3Index"/> of the center child, or <see cref="H3Index.Invalid"/> if you actually asked for a parent</returns>
    public static H3Index GetChildCenterForResolution(this H3Index origin, int childResolution) {
        var resolution = origin.Resolution;
        if (!IsValidChildResolution(resolution, childResolution)) return H3Index.Invalid;
        if (resolution == childResolution) return origin;

        H3Index childIndex = new(origin) {
            Resolution = childResolution
        };
        childIndex.ZeroDirectionsForResolutionRange(resolution + 1, childResolution);

        return childIndex;
    }

    /// <summary>
    /// Produces all child <see cref="H3Index"/> for the specified resolution.
    /// </summary>
    /// <param name="origin">index to find children for</param>
    /// <param name="childResolution">resolution of child level</param>
    /// <returns></returns>
    public static IEnumerable<H3Index> GetChildrenForResolution(this H3Index origin, int childResolution) {
        var parentResolution = origin.Resolution;
        if (!IsValidChildResolution(parentResolution, childResolution)) {
            yield break;
        }

        if (parentResolution == childResolution) {
            yield return origin;
            yield break;
        }

        // initialize our iterator by starting at the center child at the target resolution
        H3Index iterator = new(origin) {
            Resolution = childResolution
        };
        iterator.ZeroDirectionsForResolutionRange(parentResolution + 1, childResolution);

        // handle pentagons
        var fnz = iterator.IsPentagon ? childResolution : -1;

        while (iterator != H3Index.Invalid) {
            yield return new H3Index(iterator);

            var childRes = iterator.Resolution;
            iterator.IncrementDirectionForResolution(childRes);

            for (var i = childResolution; i >= parentResolution; i -= 1) {
                // done iterating?
                if (i == parentResolution) {
                    iterator = H3Index.Invalid;
                    break;
                }

                var dir = iterator.GetDirectionForResolution(i);

                // pentagon?
                if (i == fnz && dir == Direction.K) {
                    // Then we are iterating through the children of a pentagon cell.
                    // All children of a pentagon have the property that the first
                    // nonzero digit between the parent and child resolutions is
                    // not 1.
                    // I.e., we never see a sequence like 00001.
                    // Thus, we skip the `1` in this digit.
                    iterator.IncrementDirectionForResolution(i);
                    fnz -= 1;
                    break;
                }

                if (dir == Direction.Invalid) {
                    // zeros out it[i] and increments it[i-1] by 1
                    iterator.IncrementDirectionForResolution(i);
                } else {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Fills <paramref name="destination"/> with all child <see cref="H3Index"/>
    /// of <paramref name="origin"/> at the specified resolution and returns the
    /// number written.  Allocation-free equivalent of the streaming
    /// <see cref="GetChildrenForResolution(H3Index,int)"/>, producing the children
    /// in the identical order.
    /// </summary>
    /// <param name="origin">index to find children for</param>
    /// <param name="childResolution">resolution of child level</param>
    /// <param name="destination">buffer of at least
    /// <see cref="CellToChildrenSize"/>(<paramref name="origin"/>,
    /// <paramref name="childResolution"/>) cells</param>
    /// <returns>the number of children written; 0 when the child resolution is
    /// not valid for the index</returns>
    /// <exception cref="ArgumentException">Thrown when the destination buffer is
    /// smaller than <see cref="CellToChildrenSize"/>.</exception>
    public static int GetChildrenForResolution(this H3Index origin, int childResolution, Span<H3Index> destination) {
        var parentResolution = origin.Resolution;
        if (!IsValidChildResolution(parentResolution, childResolution)) {
            return 0;
        }

        if (parentResolution == childResolution) {
            if (destination.Length < 1) {
                throw new ArgumentException("destination must hold at least 1 cell", nameof(destination));
            }

            destination[0] = origin;
            return 1;
        }

        var size = origin.CellToChildrenSize(childResolution);
        if (destination.Length < size) {
            throw new ArgumentException(
                $"destination must hold at least {size} cells (see {nameof(CellToChildrenSize)})", nameof(destination));
        }

        // initialize our iterator by starting at the center child at the target
        // resolution — identical stepping to the streaming overload above
        H3Index iterator = new(origin) {
            Resolution = childResolution
        };
        iterator.ZeroDirectionsForResolutionRange(parentResolution + 1, childResolution);

        // handle pentagons
        var fnz = iterator.IsPentagon ? childResolution : -1;

        var count = 0;
        while (iterator != H3Index.Invalid) {
            destination[count++] = new H3Index(iterator);

            var childRes = iterator.Resolution;
            iterator.IncrementDirectionForResolution(childRes);

            for (var i = childResolution; i >= parentResolution; i -= 1) {
                // done iterating?
                if (i == parentResolution) {
                    iterator = H3Index.Invalid;
                    break;
                }

                var dir = iterator.GetDirectionForResolution(i);

                // pentagon?
                if (i == fnz && dir == Direction.K) {
                    iterator.IncrementDirectionForResolution(i);
                    fnz -= 1;
                    break;
                }

                if (dir == Direction.Invalid) {
                    // zeros out it[i] and increments it[i-1] by 1
                    iterator.IncrementDirectionForResolution(i);
                } else {
                    break;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Produces the number of children the <see cref="H3Index"/> has at the
    /// specified resolution.
    /// </summary>
    /// <param name="origin">index to count children for</param>
    /// <param name="childResolution">resolution of child level, must be &gt;=
    /// the index's resolution and &lt;= MAX_H3_RES</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided
    /// child resolution is not valid for the index.</exception>
    public static long CellToChildrenSize(this H3Index origin, int childResolution) {
        var parentResolution = origin.Resolution;
        if (!IsValidChildResolution(parentResolution, childResolution)) {
            throw new ArgumentOutOfRangeException(nameof(childResolution), childResolution,
                $"must be between the index's resolution ({parentResolution}) and {MAX_H3_RES}");
        }

        var n = childResolution - parentResolution;
        return origin.IsPentagon ? 1 + 5 * (IPow(7, n) - 1) / 6 : IPow(7, n);
    }

    /// <summary>
    /// Produces the position of the child <see cref="H3Index"/> within an ordered
    /// list of all children of its parent at the specified resolution.  The order
    /// of the ordered list is the same as that returned by
    /// <see cref="GetChildrenForResolution(H3Index,int)"/>.
    /// </summary>
    /// <param name="child">child index to determine the position of</param>
    /// <param name="parentResolution">parent resolution, must be &gt;= 0 and
    /// &lt;= the child's resolution</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided
    /// parent resolution is not valid for the index.</exception>
    /// <exception cref="ArgumentException">Thrown when the child index contains
    /// invalid digits.</exception>
    public static long CellToChildPos(this H3Index child, int parentResolution) {
        var childResolution = child.Resolution;
        var parent = child.GetParentForResolution(parentResolution);
        if (parent == H3Index.Invalid) {
            throw new ArgumentOutOfRangeException(nameof(parentResolution), parentResolution,
                $"must be between 0 and the child's resolution ({childResolution})");
        }

        var position = 0L;

        if (parent.IsPentagon) {
            // pentagon parents skip the 1 (K) digit, so the offsets are different
            // from those of hexagons
            for (var res = childResolution; res > parentResolution; res -= 1) {
                var parentIsPentagon = child.GetParentForResolution(res - 1).IsPentagon;
                var rawDigit = child.GetDirectionForResolution(res);
                if (rawDigit == Direction.Invalid || (parentIsPentagon && rawDigit == Direction.K)) {
                    throw new ArgumentException("contains invalid digits", nameof(child));
                }

                var digit = parentIsPentagon && rawDigit > Direction.Center ? (int)rawDigit - 1 : (int)rawDigit;
                if (digit == (int)Direction.Center) continue;

                var hexChildCount = IPow(7, childResolution - res);

                // the offset for the 0-digit slot depends on whether the current
                // index is the child of a pentagon; if so, the offset is based on
                // the count of pentagon children, otherwise, hexagon children
                position += (parentIsPentagon ? 1 + 5 * (hexChildCount - 1) / 6 : hexChildCount) +
                            (digit - 1) * hexChildCount;
            }
        } else {
            // hexagon logic, offsets are simple powers of 7
            for (var res = childResolution; res > parentResolution; res -= 1) {
                var digit = child.GetDirectionForResolution(res);
                if (digit == Direction.Invalid) {
                    throw new ArgumentException("contains invalid digits", nameof(child));
                }

                position += (int)digit * IPow(7, childResolution - res);
            }
        }

        return position;
    }

    /// <summary>
    /// Produces the child <see cref="H3Index"/> at the specified position within an
    /// ordered list of all children of the parent index at the specified resolution.
    /// The order of the ordered list is the same as that returned by
    /// <see cref="GetChildrenForResolution(H3Index,int)"/>.  This is the reverse operation of
    /// <see cref="CellToChildPos"/>.
    /// </summary>
    /// <param name="parent">parent index to produce the child of</param>
    /// <param name="position">position of the child within the parent's list of
    /// children at the specified resolution</param>
    /// <param name="childResolution">resolution of child level, must be &gt;=
    /// the parent's resolution and &lt;= MAX_H3_RES</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided
    /// child resolution is not valid for the index, or the position is out of
    /// range.</exception>
    public static H3Index ChildPosToCell(this H3Index parent, long position, int childResolution) {
        var parentResolution = parent.Resolution;

        var maxChildCount = parent.CellToChildrenSize(childResolution);
        if (position < 0 || position >= maxChildCount) {
            throw new ArgumentOutOfRangeException(nameof(position), position, $"must be between 0 and {maxChildCount - 1}");
        }

        var resolutionOffset = childResolution - parentResolution;
        var child = new H3Index(parent) {
            Resolution = childResolution
        };
        var index = position;

        if (parent.IsPentagon) {
            // pentagon parents skip the 1 (K) digit, so the offsets are different
            // from those of hexagons
            var inPentagon = true;
            for (var res = 1; res <= resolutionOffset; res += 1) {
                var resWidth = IPow(7, resolutionOffset - res);
                if (inPentagon) {
                    // while inside a parent pentagon, check if this cell is a
                    // pentagon, and if not, offset its digit to account for the
                    // skipped direction
                    var pentagonWidth = 1 + 5 * (resWidth - 1) / 6;
                    if (index < pentagonWidth) {
                        child.SetDirectionForResolution(parentResolution + res, Direction.Center);
                    } else {
                        index -= pentagonWidth;
                        inPentagon = false;
                        child.SetDirectionForResolution(parentResolution + res, (Direction)(index / resWidth + 2));
                        index %= resWidth;
                    }
                } else {
                    // no longer inside a pentagon, continue as for hexagons
                    child.SetDirectionForResolution(parentResolution + res, (Direction)(index / resWidth));
                    index %= resWidth;
                }
            }
        } else {
            // hexagon logic, offsets are simple powers of 7
            for (var res = 1; res <= resolutionOffset; res += 1) {
                var resWidth = IPow(7, resolutionOffset - res);
                child.SetDirectionForResolution(parentResolution + res, (Direction)(index / resWidth));
                index %= resWidth;
            }
        }

        return child;
    }

    /// <summary>
    /// Whether or not the parent <see cref="H3Index"/> contains the specified
    /// child <see cref="H3Index"/>; meaning, the child is equal to the parent
    /// at the parent's resolution.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="potentialChild"></param>
    /// <returns></returns>
    public static bool Contains(this H3Index parent, H3Index potentialChild) {
        var parentRes = parent.Resolution;
        if (!IsValidChildResolution(parentRes, potentialChild.Resolution)) return false;
        return potentialChild.GetParentForResolution(parentRes) == parent;
    }

    /// <summary>
    /// Whether or not the child <see cref="H3Index"/> is contained by the
    /// specified parent <see cref="H3Index"/>; meaning, the child is equal
    /// to the parent at the parent's resolution.
    /// </summary>
    /// <param name="child"></param>
    /// <param name="potentialParent"></param>
    /// <returns></returns>
    public static bool ContainedBy(this H3Index child, H3Index potentialParent) =>
        potentialParent.Contains(child);

}