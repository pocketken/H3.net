using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using H3.Extensions;
using H3.Model;

#nullable enable

namespace H3.Algorithms;

/// <summary>
/// Holder for indexes produced from the k ring functions.
/// </summary>
public readonly struct RingCell {

    public RingCell(H3Index index, int distance) {
        Index = index;
        Distance = distance;
    }

    /// <summary>
    /// H3 index
    /// </summary>
    public H3Index Index { get; }

    /// <summary>
    /// k cell distance from the origin (ring level)
    /// </summary>
    public int Distance { get; }

}

/// <summary>
/// Indicates that fast ("unsafe") k-ring traversal failed.
/// </summary>
public abstract class HexRingException : Exception { }

/// <summary>
/// Indicates that k-ring traversal failed due to the ring starting on
/// a pentagon or due to encountering indexes within the pentagon distortion
/// area.
/// </summary>
public class HexRingPentagonException : HexRingException { }

/// <summary>
/// Indicates that k-ring traversal failed due to the ring encountering
/// an index with deleted k-subsequence distortion.
/// </summary>
public class HexRingKSequenceException : HexRingException { }

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
    [Obsolete("as of 4.0: use GridRing (pentagon safe) or GridRingUnsafe instead")]
    public static IEnumerable<H3Index> GetHexRing(this H3Index origin, int k) {
        return origin.GridRingUnsafe(k);
    }

    /// <summary>
    /// Returns the "hollow" ring of cells at exactly grid distance k from
    /// the origin cell. In particular, k=0 returns just the origin cell.
    ///
    /// This function is pentagon safe: it first attempts the fast
    /// <see cref="GridRingUnsafe(H3Index,int)"/> traversal and transparently falls back to
    /// filtering <see cref="GridDiskDistancesSafe"/> when pentagonal distortion
    /// is encountered.
    ///
    /// Results are provided in no particular order.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="k"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when k is negative.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when the origin is not a valid
    /// cell index.</exception>
    public static IEnumerable<H3Index> GridRing(this H3Index origin, int k) {
        if (k < 0) {
            throw new ArgumentOutOfRangeException(nameof(k), k, "must be non-negative");
        }

        if (!origin.IsValidCell) {
            throw new ArgumentException("must be a valid cell index", nameof(origin));
        }

        try {
            List<H3Index> result = new(k == 0 ? 1 : 6 * k);
            foreach (var index in origin.GridRingUnsafe(k)) {
                result.Add(index);
            }

            return result;
        } catch (HexRingException) {
            return origin.GridDiskDistancesSafe(k)
                .Where(cell => cell.Distance == k)
                .Select(cell => cell.Index);
        }
    }

    /// <summary>
    /// Returns the "hollow" ring of cells at exactly grid distance k from
    /// the origin cell. In particular, k=0 returns just the origin cell.
    ///
    /// This function is a lower-accuracy but faster version of
    /// <see cref="GridRing"/>: a <see cref="HexRingPentagonException"/> or
    /// <see cref="HexRingKSequenceException"/> is thrown if the origin is a
    /// pentagon or pentagonal distortion is encountered during traversal.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="k"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when k is negative.
    /// </exception>
    public static IEnumerable<H3Index> GridRingUnsafe(this H3Index origin, int k) {
        if (k < 0) {
            throw new ArgumentOutOfRangeException(nameof(k), k, "must be non-negative");
        }

        // Identity short-circuit; return origin if k == 0
        if (k == 0) {
            yield return origin;
            yield break;
        }

        if (origin.IsPentagon) {
            throw new HexRingPentagonException();
        }

        var index = origin;

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
                if (pos == k - 1 && direction == 5)
                    continue;

                yield return index;
                if (index.IsPentagon) throw new HexRingPentagonException();
            }
        }

        if (lastIndex != index) throw new HexRingPentagonException();
    }

    /// <summary>
    /// Produce cells from the given origin cell within distance k.  This first
    /// attempts to use the GridDiskDistancesUnsafe method, and falls back to GridDiskDistancesSafe if
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
    [Obsolete("as of 4.0: use GridDiskDistances instead")]
    public static IEnumerable<RingCell> GetKRing(this H3Index origin, int k) {
        return origin.GridDiskDistances(k);
    }

    /// <summary>
    /// Produce cells from the given origin cell within distance k.  This first
    /// attempts to use the <see cref="GridDiskDistancesUnsafe"/> method, and falls
    /// back to <see cref="GridDiskDistancesSafe"/> if the fast method fails (e.g.
    /// pentagonal distortion).
    ///
    /// k-ring 0 is defined as the origin cell, k-ring 1 is defined as k-ring 0 and
    /// all neighboring cells, and so on.
    ///
    /// Results are provided in no particular order.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="k"></param>
    /// <returns></returns>
    public static IEnumerable<RingCell> GridDiskDistances(this H3Index origin, int k) {
        try {
            List<RingCell> result = new(3 * k * (k + 1) + 1);
            GridDiskDistancesUnsafeInto(origin, k, result);
            return result;
        } catch (HexRingException) {
            return origin.GridDiskDistancesSafe(k);
        }
    }

    /// <summary>
    /// Eager, non-iterator equivalent of <see cref="GridDiskDistancesUnsafe"/>
    /// that writes cells directly into <paramref name="result"/> instead of
    /// yielding them.  This avoids allocating an iterator state-machine object
    /// on the hot <see cref="GridDiskDistances(H3Index,int)"/> path, which always drains the
    /// full disk into a list anyway.  The traversal, cell ordering and
    /// pentagon/k-subsequence exception semantics are identical to
    /// <see cref="GridDiskDistancesUnsafe"/>.
    /// </summary>
    private static void GridDiskDistancesUnsafeInto(H3Index origin, int k, List<RingCell> result) {
        var index = origin;

        // k must be >= 0, so origin is always needed
        result.Add(new RingCell(index, 0));

        // Pentagon was encountered; bail out as user doesn't want this.
        if (index.IsPentagon) throw new HexRingPentagonException();

        // short circuit; k = 0 means we just want the origin (strange, but you get what you ask for)
        if (k == 0) return;

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

            result.Add(new RingCell(index, ring));
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

    /// <summary>
    /// Iteratively produces indexes within k cell distance of the origin index.  This
    /// is a higher-accuracy but slower version of <see cref="GridDiskDistancesUnsafe"/>.
    ///
    /// k-ring 0 is defined as the origin index, k-ring 1 is defined as k-ring 0 and
    /// all neighboring indexes, and so on.
    /// </summary>
    /// <param name="origin">Origin location</param>
    /// <param name="k">k >= 0</param>
    /// <returns>all neighbours within k cell distance</returns>
    [Obsolete("as of 4.0: use GridDiskDistancesSafe instead")]
    public static IEnumerable<RingCell> GetKRingSlow(this H3Index origin, int k) {
        return origin.GridDiskDistancesSafe(k);
    }

    /// <summary>
    /// Iteratively produces indexes within k cell distance of the origin index.  This
    /// is a higher-accuracy but slower version of <see cref="GridDiskDistancesUnsafe"/>.
    ///
    /// k-ring 0 is defined as the origin index, k-ring 1 is defined as k-ring 0 and
    /// all neighboring indexes, and so on.
    /// </summary>
    /// <param name="origin">Origin location</param>
    /// <param name="k">k >= 0</param>
    /// <returns>all neighbours within k cell distance</returns>
    public static IEnumerable<RingCell> GridDiskDistancesSafe(this H3Index origin, int k) {
        // if not a valid index then nothing to do
        if (origin == H3Index.Invalid) yield break;

        // the number of cells within distance k of the origin is at most
        // 3 * k * (k + 1) + 1 (and no more than the total number of cells at
        // the origin's resolution), so each unique cell enters the
        // breadth-first queue exactly once and the queue never needs to grow
        var totalCells = 2L + 120L * Utils.IPow(7, origin.Resolution);
        var maximumSize = (int)Math.Min(k < 1_000_000 ? 3L * k * (k + 1) + 1 : long.MaxValue, Math.Min(totalCells, 1 << 30));
        var cells = new RingCell[maximumSize];
        var count = 0;

        // cell values are never 0, so 0 can mark an empty slot in the
        // open-addressed dedup table; the table always retains at least one
        // empty slot as it holds at most maximumSize - 1 entries
        var tableSize = 4;
        while (tableSize < 1 << 30 && tableSize < maximumSize) tableSize <<= 1;
        var searched = new ulong[tableSize];

        // since k >= 0, start with origin
        cells[count] = new RingCell(origin, 0);
        count += 1;

        for (var head = 0; head < count; head += 1) {
            var cell = cells[head];
            yield return cell;

            var nextK = cell.Distance + 1;
            if (nextK > k)
                continue;

            for (var d = Direction.K; d < Direction.Invalid; d += 1) {
                var (neighbour, _) = cell.Index.GetDirectNeighbour(d);
                if (neighbour == H3Index.Invalid || neighbour == origin || neighbour == cell.Index) {
                    continue;
                }

                if (!TryAddToProbeTable(searched, neighbour)) {
                    continue;
                }

                cells[count] = new RingCell(neighbour, nextK);
                count += 1;
            }
        }
    }

    /// <summary>
    /// Adds a value to an open-addressed, linear-probed power-of-two-sized
    /// table, returning false if the value was already present.  The value
    /// must be non-zero; zero marks empty slots.
    /// </summary>
    private static bool TryAddToProbeTable(Span<ulong> table, ulong value) {
        var mask = table.Length - 1;
        var slot = (int)((value * 0x9E3779B97F4A7C15UL) >> 32) & mask;

        while (true) {
            var existing = table[slot];
            if (existing == 0) {
                table[slot] = value;
                return true;
            }

            if (existing == value) {
                return false;
            }

            slot = (slot + 1) & mask;
        }
    }

    /// <summary>
    /// Produces indexes within k cell distance of the origin index.  This is a
    /// lower-accuracy but faster version of <see cref="GridDiskDistancesSafe"/>.
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
    [Obsolete("as of 4.0: use GridDiskDistancesUnsafe instead")]
    public static IEnumerable<RingCell> GetKRingFast(this H3Index origin, int k) {
        return origin.GridDiskDistancesUnsafe(k);
    }

    /// <summary>
    /// Produces indexes within k cell distance of the origin index.  This is a
    /// lower-accuracy but faster version of <see cref="GridDiskDistancesSafe"/>.
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
    public static IEnumerable<RingCell> GridDiskDistancesUnsafe(this H3Index origin, int k) {
        var index = origin;

        // k must be >= 0, so origin is always needed
        yield return new RingCell(index, 0);

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

            yield return new RingCell(index, ring);
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

    // ------------------------------------------------------------------
    // Zero-allocation span / buffer-fill overloads (additive; the streaming
    // IEnumerable API above is unchanged).  Each fill method writes cells into a
    // caller-owned Span and returns the number written; size the destination via
    // MaxGridDiskSize / MaxGridRingSize (mirrors libh3's maxGridDiskSize).  The
    // traversal, ordering and pentagon/k-subsequence semantics are identical to
    // the corresponding streaming method above (locked by parity tests).
    // ------------------------------------------------------------------

    /// <summary>
    /// The maximum number of cells produced by a grid disk of radius
    /// <paramref name="k"/>, i.e. the minimum length of the destination buffer
    /// for the <see cref="Span{T}"/> overloads of <see cref="GridDisk"/> and
    /// <see cref="GridDiskDistances(H3Index,int,Span{RingCell})"/>.  Equal to
    /// <c>3·k·(k+1)+1</c>.
    /// </summary>
    /// <param name="k">disk radius; must be non-negative</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when k is negative.</exception>
    public static int MaxGridDiskSize(int k) {
        if (k < 0) throw new ArgumentOutOfRangeException(nameof(k), k, "must be non-negative");
        return 3 * k * (k + 1) + 1;
    }

    /// <summary>
    /// The maximum number of cells produced by the hollow grid ring at distance
    /// <paramref name="k"/>, i.e. the minimum length of the destination buffer
    /// for the <see cref="Span{T}"/> overload of
    /// <see cref="GridRingUnsafe(H3Index,int,Span{H3Index})"/>.  Equal to
    /// <c>1</c> when k is 0, otherwise <c>6·k</c>.
    /// </summary>
    /// <param name="k">ring distance; must be non-negative</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when k is negative.</exception>
    public static int MaxGridRingSize(int k) {
        if (k < 0) throw new ArgumentOutOfRangeException(nameof(k), k, "must be non-negative");
        return k == 0 ? 1 : 6 * k;
    }

    /// <summary>
    /// Fills <paramref name="destination"/> with the cells within grid distance
    /// <paramref name="k"/> of <paramref name="origin"/> (no distances) and
    /// returns the number of cells written.  Pentagon safe: attempts the fast
    /// traversal and transparently falls back to the breadth-first traversal
    /// when pentagonal distortion is encountered.  Allocation-free on the fast
    /// path; the fallback rents its scratch from <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="origin">origin cell</param>
    /// <param name="k">disk radius; must be non-negative</param>
    /// <param name="destination">buffer of at least
    /// <see cref="MaxGridDiskSize"/>(<paramref name="k"/>) cells</param>
    /// <returns>the number of cells written to <paramref name="destination"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when k is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when the destination buffer is
    /// smaller than <see cref="MaxGridDiskSize"/>.</exception>
    public static int GridDisk(this H3Index origin, int k, Span<H3Index> destination) {
        var required = MaxGridDiskSize(k);
        if (destination.Length < required) {
            throw new ArgumentException(
                $"destination must hold at least {required} cells (see {nameof(MaxGridDiskSize)})", nameof(destination));
        }

        // The cells-only fast path shares the RingCell traversal via a pooled
        // scratch buffer, then projects the indexes into the caller's span.
        var scratch = ArrayPool<RingCell>.Shared.Rent(required);
        try {
            var cells = scratch.AsSpan(0, required);
            int count;
            try {
                count = GridDiskDistancesUnsafeInto(origin, k, cells);
            } catch (HexRingException) {
                count = GridDiskDistancesSafeInto(origin, k, cells);
            }

            for (var i = 0; i < count; i += 1) destination[i] = cells[i].Index;
            return count;
        } finally {
            ArrayPool<RingCell>.Shared.Return(scratch);
        }
    }

    /// <summary>
    /// Fills <paramref name="destination"/> with the <see cref="RingCell"/>s
    /// (cell plus ring distance) within grid distance <paramref name="k"/> of
    /// <paramref name="origin"/> and returns the number written.  Pentagon safe,
    /// with cells, distances and ordering identical to the streaming
    /// <see cref="GridDiskDistances(H3Index,int)"/>.  Allocation-free on the fast
    /// path; the fallback rents its dedup table from <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="origin">origin cell</param>
    /// <param name="k">disk radius; must be non-negative</param>
    /// <param name="destination">buffer of at least
    /// <see cref="MaxGridDiskSize"/>(<paramref name="k"/>) cells</param>
    /// <returns>the number of cells written to <paramref name="destination"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when k is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when the destination buffer is
    /// smaller than <see cref="MaxGridDiskSize"/>.</exception>
    public static int GridDiskDistances(this H3Index origin, int k, Span<RingCell> destination) {
        var required = MaxGridDiskSize(k);
        if (destination.Length < required) {
            throw new ArgumentException(
                $"destination must hold at least {required} cells (see {nameof(MaxGridDiskSize)})", nameof(destination));
        }

        try {
            return GridDiskDistancesUnsafeInto(origin, k, destination);
        } catch (HexRingException) {
            return GridDiskDistancesSafeInto(origin, k, destination);
        }
    }

    /// <summary>
    /// Fills <paramref name="destination"/> with the hollow ring of cells at
    /// exactly grid distance <paramref name="k"/> from <paramref name="origin"/>
    /// and returns the number written.  Allocation-free equivalent of the
    /// streaming <see cref="GridRingUnsafe(H3Index,int)"/>; not pentagon safe: a
    /// <see cref="HexRingException"/> is thrown if the origin is a pentagon or
    /// pentagonal distortion is encountered.
    /// </summary>
    /// <param name="origin">origin cell</param>
    /// <param name="k">ring distance; must be non-negative</param>
    /// <param name="destination">buffer of at least
    /// <see cref="MaxGridRingSize"/>(<paramref name="k"/>) cells</param>
    /// <returns>the number of cells written to <paramref name="destination"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when k is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when the destination buffer is
    /// smaller than <see cref="MaxGridRingSize"/>.</exception>
    /// <exception cref="HexRingException">Thrown when pentagonal distortion is
    /// encountered.</exception>
    public static int GridRingUnsafe(this H3Index origin, int k, Span<H3Index> destination) {
        var required = MaxGridRingSize(k);
        if (destination.Length < required) {
            throw new ArgumentException(
                $"destination must hold at least {required} cells (see {nameof(MaxGridRingSize)})", nameof(destination));
        }

        return GridRingUnsafeInto(origin, k, destination);
    }

    /// <summary>
    /// Span-filling core mirroring the traversal, ordering and exception
    /// semantics of <see cref="GridDiskDistancesUnsafe"/> exactly, writing into
    /// <paramref name="destination"/> instead of yielding.
    /// </summary>
    private static int GridDiskDistancesUnsafeInto(H3Index origin, int k, Span<RingCell> destination) {
        var index = origin;
        var count = 0;

        // k must be >= 0, so origin is always needed
        destination[count++] = new RingCell(index, 0);

        // Pentagon was encountered; bail out as user doesn't want this.
        if (index.IsPentagon) throw new HexRingPentagonException();

        // short circuit; k = 0 means we just want the origin
        if (k == 0) return count;

        var ring = 1;
        var direction = 0;
        var i = 0;
        var rotations = 0;

        while (ring <= k) {
            if (direction == 0 && i == 0) {
                (index, rotations) = index.GetDirectNeighbour(LookupTables.NextRingDirection, rotations);
                if (index == H3Index.Invalid) throw new HexRingKSequenceException();
                if (index.IsPentagon) throw new HexRingPentagonException();
            }

            (index, rotations) = index.GetDirectNeighbour(LookupTables.CounterClockwiseDirections[direction], rotations);
            if (index == H3Index.Invalid) throw new HexRingKSequenceException();

            destination[count++] = new RingCell(index, ring);
            i += 1;

            if (i == ring) {
                i = 0;
                direction += 1;
                if (direction == 6) {
                    direction = 0;
                    ring += 1;
                }
            }

            if (index.IsPentagon) throw new HexRingPentagonException();
        }

        return count;
    }

    /// <summary>
    /// Span-filling core mirroring <see cref="GridDiskDistancesSafe"/>: the
    /// caller's <paramref name="destination"/> doubles as the breadth-first
    /// queue (it is sized to the disk maximum) and the linear-probe dedup table
    /// is rented from <see cref="ArrayPool{T}"/>.  Cells, distances and ordering
    /// are identical to the streaming safe traversal.
    /// </summary>
    private static int GridDiskDistancesSafeInto(H3Index origin, int k, Span<RingCell> destination) {
        if (origin == H3Index.Invalid) return 0;

        var totalCells = 2L + 120L * Utils.IPow(7, origin.Resolution);
        var maximumSize = (int)Math.Min(k < 1_000_000 ? 3L * k * (k + 1) + 1 : long.MaxValue, Math.Min(totalCells, 1 << 30));
        var tableSize = 4;
        while (tableSize < 1 << 30 && tableSize < maximumSize) tableSize <<= 1;

        var searched = ArrayPool<ulong>.Shared.Rent(tableSize);
        try {
            Array.Clear(searched, 0, tableSize);
            var table = searched.AsSpan(0, tableSize);

            var count = 0;
            destination[count++] = new RingCell(origin, 0);

            for (var head = 0; head < count; head += 1) {
                var cell = destination[head];
                var nextK = cell.Distance + 1;
                if (nextK > k) continue;

                for (var d = Direction.K; d < Direction.Invalid; d += 1) {
                    var (neighbour, _) = cell.Index.GetDirectNeighbour(d);
                    if (neighbour == H3Index.Invalid || neighbour == origin || neighbour == cell.Index) {
                        continue;
                    }

                    if (!TryAddToProbeTable(table, neighbour)) {
                        continue;
                    }

                    destination[count++] = new RingCell(neighbour, nextK);
                }
            }

            return count;
        } finally {
            ArrayPool<ulong>.Shared.Return(searched);
        }
    }

    /// <summary>
    /// Span-filling core mirroring the traversal, ordering and exception
    /// semantics of <see cref="GridRingUnsafe(H3Index,int)"/> exactly.
    /// </summary>
    private static int GridRingUnsafeInto(H3Index origin, int k, Span<H3Index> destination) {
        var count = 0;

        // Identity short-circuit; return origin if k == 0
        if (k == 0) {
            destination[count++] = origin;
            return count;
        }

        if (origin.IsPentagon) throw new HexRingPentagonException();

        var index = origin;
        var rotations = 0;

        // break out to the requested ring
        for (var ring = 0; ring < k; ring += 1) {
            (index, rotations) = index.GetDirectNeighbour(LookupTables.NextRingDirection, rotations);
            if (index == H3Index.Invalid) throw new HexRingKSequenceException();
            if (index.IsPentagon) throw new HexRingPentagonException();
        }

        H3Index lastIndex = new(index);
        destination[count++] = index;

        for (var direction = 0; direction < 6; direction += 1) {
            for (var pos = 0; pos < k; pos += 1) {
                (index, rotations) = index.GetDirectNeighbour(LookupTables.CounterClockwiseDirections[direction], rotations);
                if (index == H3Index.Invalid) throw new HexRingKSequenceException();

                // Skip the very last index, it was already added; still traverse
                // to it for the pentagonal distortion check below.
                if (pos == k - 1 && direction == 5) continue;

                destination[count++] = index;
                if (index.IsPentagon) throw new HexRingPentagonException();
            }
        }

        if (lastIndex != index) throw new HexRingPentagonException();
        return count;
    }

}