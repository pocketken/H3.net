using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using H3.Model;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Extensions;

/// <summary>
/// Provides extension methods that operate on sets of H3Index.
/// </summary>
public static class H3SetExtensions {

    /// <summary>
    /// Takes a set of cells and compacts them by removing duplicates and
    /// pruning full child branches to the parent level. This is also done for
    /// all parents recursively to get the minimum number of indexes that perfectly
    /// cover the defined space.</summary>
    /// <remarks>This implementation differs from upstream in that mixed resolutions
    /// are supported, and duplicate or invalid inputs are filtered instead returning
    /// an error code when they are encountered.  Based on the "FlexiCompact" method
    /// in H3Lib
    /// (https://github.com/RichardVasquez/h3net/blob/v3.7.1/H3Lib/Extensions/H3LibExtensions.cs#L359)
    /// </remarks>
    /// <param name="indexEnumerable">set of cells to compact</param>
    /// <returns>set of compacted cells</returns>
    [Obsolete("as of 4.0: Use CompactCells instead")]
    public static List<H3Index> Compact(this IEnumerable<H3Index> indexEnumerable) {
        return indexEnumerable.CompactCells();
    }

    /// <summary>
    /// Takes a set of cells and compacts them by removing duplicates and
    /// pruning full child branches to the parent level. This is also done for
    /// all parents recursively to get the minimum number of indexes that perfectly
    /// cover the defined space.</summary>
    /// <remarks>This implementation differs from upstream in that mixed resolutions
    /// are supported, and duplicate or invalid inputs are filtered instead returning
    /// an error code when they are encountered.  Based on the "FlexiCompact" method
    /// in H3Lib
    /// (https://github.com/RichardVasquez/h3net/blob/v3.7.1/H3Lib/Extensions/H3LibExtensions.cs#L359)
    /// </remarks>
    /// <param name="indexEnumerable">set of cells to compact</param>
    /// <returns>set of compacted cells</returns>
    public static List<H3Index> CompactCells(this IEnumerable<H3Index> indexEnumerable) {
        var byResolution = new List<H3Index>?[MAX_H3_RES + 1];
        var maxResolution = -1;
        var count = 0;

        // Compaction inputs are overwhelmingly single-resolution, so presize the
        // first per-resolution bucket to the input size (when known).  That lets
        // the dominant bucket fill without the repeated doubling — and discarding —
        // of its backing array, which otherwise accounts for roughly half of the
        // garbage produced here.  Only the first bucket is hinted, bounding
        // worst-case over-allocation for mixed-resolution inputs to <= Count.
        var sizeHint = indexEnumerable is ICollection<H3Index> collection ? collection.Count : 0;
        var hintUsed = false;

        // first group by resolution
        foreach (var index in indexEnumerable) {
            if (index == H3Index.Invalid) {
                continue;
            }

            var indexResolution = index.Resolution;
            if (indexResolution > maxResolution) maxResolution = indexResolution;

            var bucket = byResolution[indexResolution];
            if (bucket == null) {
                bucket = new List<H3Index>(hintUsed ? 0 : sizeHint);
                hintUsed = true;
                byResolution[indexResolution] = bucket;
            }

            bucket.Add(index);
            count++;
        }

        // worst case, nothing gets compacted
        List<H3Index> results = new(count);

        // loop backward through each resolution, throwing any compacted parents
        // into the resolution below us.  Cells that share a parent are adjacent
        // once sorted, so complete sets of children (and duplicates) can be
        // detected with a linear scan.
        for (var resolution = maxResolution; resolution > 0; resolution -= 1) {
            var toCompact = byResolution[resolution];
            if (toCompact == null)
                continue;

            SortByValueAscending(toCompact);
            var parentResolution = resolution - 1;
            var total = toCompact.Count;
            var i = 0;

            while (i < total) {
                var parentValue = toCompact[i].GetParentValueForResolution(parentResolution);

                // count the distinct children sharing this parent
                var runStart = i;
                var childCount = 0;
                var previous = H3Index.Invalid;
                while (i < total && toCompact[i].GetParentValueForResolution(parentResolution) == parentValue) {
                    if (toCompact[i] != previous) {
                        childCount += 1;
                        previous = toCompact[i];
                    }

                    i += 1;
                }

                var parent = new H3Index(parentValue);
                if (childCount >= (parent.IsPentagon ? 6 : 7)) {
                    (byResolution[parentResolution] ??= new List<H3Index>()).Add(parent);
                } else {
                    previous = H3Index.Invalid;
                    for (var j = runStart; j < i; j += 1) {
                        if (toCompact[j] == previous) continue;
                        results.Add(toCompact[j]);
                        previous = toCompact[j];
                    }
                }
            }
        }

        // and lastly, add in any res 0
        var zeroes = byResolution[0];
        if (zeroes != null) {
            SortByValueAscending(zeroes);
            for (var j = 0; j < zeroes.Count; j += 1) {
                if (j != 0 && zeroes[j] == zeroes[j - 1]) continue;
                results.Add(zeroes[j]);
            }
        }

        return results;
    }

    /// <summary>
    /// Takes a compacted set of cells and expands back to the original
    /// set of cells at a specific resolution.
    /// </summary>
    /// <param name="indexes">set of cells</param>
    /// <param name="resolution">resolution to expand to</param>
    /// <returns>original set of cells. Throws ArgumentException if any
    /// cell in the set is smaller than the output resolution or invalid
    /// resolution is requested.</returns>
    [Obsolete("as of 4.0: use UncompactCells instead")]
    public static IEnumerable<H3Index> UncompactToResolution(this IEnumerable<H3Index> indexes, int resolution) {
        return indexes.UncompactCells(resolution);
    }

    /// <summary>
    /// Takes a compacted set of cells and expands back to the original
    /// set of cells at a specific resolution.
    /// </summary>
    /// <param name="indexes">set of cells</param>
    /// <param name="resolution">resolution to expand to</param>
    /// <returns>original set of cells. Throws ArgumentException if any
    /// cell in the set is smaller than the output resolution or invalid
    /// resolution is requested.</returns>
    public static IEnumerable<H3Index> UncompactCells(this IEnumerable<H3Index> indexes, int resolution) {
        // Dedup the (valid) input cells exactly as the previous HashSet<H3Index>
        // did, but with an open-addressed, linear-probed ulong table rented from
        // ArrayPool instead: cell values are never zero (H3_NULL is filtered
        // below before being added, so zero safely marks an empty slot).  This
        // mirrors the probe table the polyfill flood fill (Polyfill.SearchedSet)
        // and the safe grid-disk traversal (Rings.TryAddToProbeTable) already use
        // in place of a hash set — a single contiguous backing array both
        // allocates far less than the hash set's bucket + entry arrays (and is
        // returned to the pool) and probes faster on the per-input membership
        // test, the hottest operation here.  The expansion is still streamed
        // through this single iterator (no per-cell child iterator).  Produced
        // cells, their order, dedup and the (lazy) exception behaviour are
        // identical to the previous implementation.
        var seen = new SeenCellSet(indexes is ICollection<H3Index> collection ? collection.Count : 0);
        try {
            foreach (var index in indexes) {
                if (index == H3Index.Invalid || !seen.Add(index)) continue;

                var currentResolution = index.Resolution;
                if (!IsValidChildResolution(currentResolution, resolution)) {
                    throw new ArgumentException("set contains cell smaller than target resolution");
                }

                // same resolution: the cell is its own only child
                if (currentResolution == resolution) {
                    yield return index;
                    continue;
                }

                // inlined equivalent of index.GetChildrenForResolution(resolution),
                // producing the children in the identical order

                // initialize our iterator by starting at the center child at the
                // target resolution
                H3Index iterator = new(index) {
                    Resolution = resolution
                };
                iterator.ZeroDirectionsForResolutionRange(currentResolution + 1, resolution);

                // handle pentagons
                var fnz = iterator.IsPentagon ? resolution : -1;

                while (iterator != H3Index.Invalid) {
                    yield return new H3Index(iterator);

                    var childRes = iterator.Resolution;
                    iterator.IncrementDirectionForResolution(childRes);

                    for (var i = resolution; i >= currentResolution; i -= 1) {
                        // done iterating?
                        if (i == currentResolution) {
                            iterator = H3Index.Invalid;
                            break;
                        }

                        var dir = iterator.GetDirectionForResolution(i);

                        // pentagon?
                        if (i == fnz && dir == Direction.K) {
                            // Then we are iterating through the children of a pentagon
                            // cell.  All children of a pentagon have the property that
                            // the first nonzero digit between the parent and child
                            // resolutions is not 1.  I.e., we never see a sequence like
                            // 00001.  Thus, we skip the `1` in this digit.
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
        } finally {
            seen.Dispose();
        }
    }

    /// <summary>
    /// Takes a set of indexes and expands to the highest found resolution
    /// within the set.
    /// </summary>
    /// <param name="indexes"></param>
    /// <returns>expanded set ofindexes</returns>
    [Obsolete("as of 4.0: use UncompactCellsToHighestResolution instead")]
    public static IEnumerable<H3Index> UncompactToHighestResolution(this IEnumerable<H3Index> indexes) =>
        UncompactCells(indexes, indexes.Max(i => i.Resolution));

    /// <summary>
    /// Takes a set of indexes and expands to the highest found resolution
    /// within the set.
    /// </summary>
    /// <param name="indexes"></param>
    /// <returns>expanded set ofindexes</returns>
    public static IEnumerable<H3Index> UncompactCellsToHighestResolution(this IEnumerable<H3Index> indexes) =>
        UncompactCells(indexes, indexes.Max(i => i.Resolution));

    /// <summary>
    /// Produces the canonical form of the provided set of cells: sorted
    /// ascending by index value with duplicates and H3_NULL entries removed.
    /// Canonical sets support fast binary-search based containment queries
    /// via <see cref="CanonicalCellsContain"/>.
    /// </summary>
    /// <param name="cells">set of cells; may contain mixed resolutions, e.g.
    /// the output of <see cref="CompactCells(IEnumerable{H3Index})"/></param>
    /// <returns>canonicalized set of cells</returns>
    public static List<H3Index> CanonicalizeCells(this IEnumerable<H3Index> cells) {
        List<H3Index> result = cells is IReadOnlyCollection<H3Index> collection ? new(collection.Count) : new();

        foreach (var cell in cells) {
            if (cell != H3Index.Invalid) result.Add(cell);
        }

        SortByValueAscending(result);

        var write = 0;
        for (var read = 0; read < result.Count; read += 1) {
            if (read != 0 && result[read] == result[write - 1]) continue;
            result[write] = result[read];
            write += 1;
        }

        result.RemoveRange(write, result.Count - write);
        return result;
    }

    /// <summary>
    /// Determines whether or not the provided set of cells is canonical, i.e.
    /// sorted ascending by index value and free of duplicates and H3_NULL
    /// entries.
    /// </summary>
    /// <param name="cells">set of cells</param>
    /// <returns>true if the set is canonical</returns>
    public static bool IsCanonicalCells(this IReadOnlyList<H3Index> cells) {
        for (var i = 0; i < cells.Count; i += 1) {
            if (cells[i] == H3Index.Invalid) return false;
            if (i != 0 && cells[i - 1].CompareTo(cells[i]) >= 0) return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether or not the canonical set of cells contains the
    /// provided cell, either exactly or via one of the cell's ancestors;
    /// i.e. whether the area covered by the set contains the cell.  Performs
    /// a binary search per resolution, i.e. O(res * log n), without
    /// materializing the covered area.
    /// </summary>
    /// <param name="canonicalCells">canonical set of cells (see
    /// <see cref="CanonicalizeCells"/>); may contain mixed resolutions, e.g.
    /// the output of <see cref="CompactCells(IEnumerable{H3Index})"/></param>
    /// <param name="cell">cell to test</param>
    /// <returns>true if the cell or one of its ancestors is present within
    /// the set</returns>
    public static bool CanonicalCellsContain(this IReadOnlyList<H3Index> canonicalCells, H3Index cell) {
        if (canonicalCells.Count == 0 || cell == H3Index.Invalid) return false;

        for (var resolution = cell.Resolution; resolution >= 0; resolution -= 1) {
            var candidate = new H3Index(cell.GetParentValueForResolution(resolution));

            var low = 0;
            var high = canonicalCells.Count - 1;
            while (low <= high) {
                var mid = low + ((high - low) >> 1);
                var comparison = canonicalCells[mid].CompareTo(candidate);
                if (comparison == 0) return true;
                if (comparison < 0) {
                    low = mid + 1;
                } else {
                    high = mid - 1;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether or not all H3Index entries within the enumerable are
    /// of the same resolution.
    /// </summary>
    /// <param name="indexes">set of cells</param>
    /// <returns>true if all cells are of the same resolution, false if
    /// not.
    /// </returns>
    public static bool AreOfSameResolution(this IEnumerable<H3Index> indexes) {
        var resolution = -1;
        foreach (var index in indexes) {
            if (resolution == -1) {
                resolution = index.Resolution;
            } else {
                if (resolution != index.Resolution) {
                    return false;
                }
            }
        }
        return true;
    }

    // ------------------------------------------------------------------
    // Zero-allocation span / buffer-fill overloads (additive; the streaming
    // IEnumerable API above is unchanged).  Inputs are taken as ReadOnlySpan and
    // results written into a caller-owned Span; internal scratch is rented from
    // ArrayPool so a warm pool makes these allocation-free.  Produced cells and
    // their order are identical to the corresponding streaming method (locked by
    // parity tests).
    // ------------------------------------------------------------------

    /// <summary>
    /// Compacts <paramref name="cells"/> into <paramref name="destination"/> and
    /// returns the number of cells written.  Allocation-free (pooled scratch)
    /// equivalent of the streaming <see cref="CompactCells(IEnumerable{H3Index})"/>:
    /// duplicates and complete child branches are pruned to their parent, mixed
    /// resolutions are supported, and invalid inputs are filtered — producing the
    /// identical set in the identical order.  Because compaction never produces
    /// more cells than it consumes, a destination of length <c>cells.Length</c> is
    /// always sufficient.
    /// </summary>
    /// <param name="cells">set of cells to compact (mixed resolutions allowed)</param>
    /// <param name="destination">buffer of at least <c>cells.Length</c> cells</param>
    /// <returns>the number of compacted cells written to
    /// <paramref name="destination"/></returns>
    /// <exception cref="ArgumentException">Thrown when the destination buffer is
    /// smaller than <paramref name="cells"/>.</exception>
    public static int CompactCells(ReadOnlySpan<H3Index> cells, Span<H3Index> destination) {
        if (destination.Length < cells.Length) {
            throw new ArgumentException(
                $"destination must hold at least {cells.Length} cells (compaction never grows the set)", nameof(destination));
        }

        // per-resolution buckets, backed by pooled arrays; a plain array (not a
        // List) so `buckets[r].Add(...)` mutates the struct element in place
        var buckets = ArrayPool<PooledCellList>.Shared.Rent(MAX_H3_RES + 1);
        try {
            // rented array contents are undefined and may carry stale pooled-array
            // references from a prior rental; clear to fresh (null, 0) structs
            Array.Clear(buckets, 0, MAX_H3_RES + 1);

            var maxResolution = -1;

            // first group by resolution
            foreach (var index in cells) {
                if (index == H3Index.Invalid) continue;

                var indexResolution = index.Resolution;
                if (indexResolution > maxResolution) maxResolution = indexResolution;
                buckets[indexResolution].Add(index);
            }

            var count = 0;

            // loop backward through each resolution, throwing any compacted parents
            // into the resolution below us.  Cells that share a parent are adjacent
            // once sorted, so complete sets of children (and duplicates) can be
            // detected with a linear scan.
            for (var resolution = maxResolution; resolution > 0; resolution -= 1) {
                if (buckets[resolution].Count == 0) continue;

                buckets[resolution].Sort();
                var parentResolution = resolution - 1;
                var total = buckets[resolution].Count;
                var i = 0;

                while (i < total) {
                    var parentValue = buckets[resolution][i].GetParentValueForResolution(parentResolution);

                    // count the distinct children sharing this parent
                    var runStart = i;
                    var childCount = 0;
                    var previous = H3Index.Invalid;
                    while (i < total && buckets[resolution][i].GetParentValueForResolution(parentResolution) == parentValue) {
                        if (buckets[resolution][i] != previous) {
                            childCount += 1;
                            previous = buckets[resolution][i];
                        }

                        i += 1;
                    }

                    var parent = new H3Index(parentValue);
                    if (childCount >= (parent.IsPentagon ? 6 : 7)) {
                        buckets[parentResolution].Add(parent);
                    } else {
                        previous = H3Index.Invalid;
                        for (var j = runStart; j < i; j += 1) {
                            if (buckets[resolution][j] == previous) continue;
                            destination[count++] = buckets[resolution][j];
                            previous = buckets[resolution][j];
                        }
                    }
                }
            }

            // and lastly, add in any res 0
            if (buckets[0].Count > 0) {
                buckets[0].Sort();
                for (var j = 0; j < buckets[0].Count; j += 1) {
                    if (j != 0 && buckets[0][j] == buckets[0][j - 1]) continue;
                    destination[count++] = buckets[0][j];
                }
            }

            return count;
        } finally {
            for (var r = 0; r <= MAX_H3_RES; r += 1) buckets[r].Dispose();
            ArrayPool<PooledCellList>.Shared.Return(buckets);
        }
    }

    /// <summary>
    /// The maximum number of cells produced by expanding the compacted set
    /// <paramref name="compactedCells"/> to <paramref name="resolution"/>, i.e. an
    /// upper bound on (and, for a duplicate-free input, the exact size of) the
    /// destination buffer for the <see cref="Span{T}"/> overload of
    /// <see cref="UncompactCells(ReadOnlySpan{H3Index},int,Span{H3Index})"/>.
    /// Equal to the sum of <see cref="H3HierarchyExtensions.CellToChildrenSize"/>
    /// over the (non-invalid) input cells.
    /// </summary>
    /// <param name="compactedCells">compacted set of cells</param>
    /// <param name="resolution">resolution to expand to</param>
    /// <returns>upper bound on the uncompacted cell count</returns>
    /// <exception cref="ArgumentException">Thrown when any cell in the set is finer
    /// than <paramref name="resolution"/>.</exception>
    public static long UncompactCellsSize(ReadOnlySpan<H3Index> compactedCells, int resolution) {
        long total = 0;
        foreach (var index in compactedCells) {
            if (index == H3Index.Invalid) continue;

            var currentResolution = index.Resolution;
            if (!IsValidChildResolution(currentResolution, resolution)) {
                throw new ArgumentException("set contains cell smaller than target resolution");
            }

            total += index.CellToChildrenSize(resolution);
        }

        return total;
    }

    /// <summary>
    /// Expands the compacted set <paramref name="compactedCells"/> to
    /// <paramref name="resolution"/>, writing the result into
    /// <paramref name="destination"/> and returning the number of cells written.
    /// Allocation-free (pooled dedup scratch) equivalent of the streaming
    /// <see cref="UncompactCells(IEnumerable{H3Index},int)"/>, producing the
    /// identical cells in the identical order.  Size the destination with
    /// <see cref="UncompactCellsSize"/>.
    /// </summary>
    /// <param name="compactedCells">compacted set of cells</param>
    /// <param name="resolution">resolution to expand to</param>
    /// <param name="destination">buffer of at least
    /// <see cref="UncompactCellsSize"/>(<paramref name="compactedCells"/>,
    /// <paramref name="resolution"/>) cells</param>
    /// <returns>the number of cells written to <paramref name="destination"/></returns>
    /// <exception cref="ArgumentException">Thrown when any cell in the set is finer
    /// than <paramref name="resolution"/>, or the destination is too small.</exception>
    public static int UncompactCells(ReadOnlySpan<H3Index> compactedCells, int resolution, Span<H3Index> destination) {
        // dedup the (valid) input cells exactly as the streaming overload does,
        // then expand each unique cell's children directly into the destination via
        // the hierarchy span fill (identical child order)
        var seen = new SeenCellSet(compactedCells.Length);
        try {
            var count = 0;
            foreach (var index in compactedCells) {
                if (index == H3Index.Invalid || !seen.Add(index)) continue;

                var currentResolution = index.Resolution;
                if (!IsValidChildResolution(currentResolution, resolution)) {
                    throw new ArgumentException("set contains cell smaller than target resolution");
                }

                count += index.GetChildrenForResolution(resolution, destination[count..]);
            }

            return count;
        } finally {
            seen.Dispose();
        }
    }

    /// <summary>
    /// Sorts a list of cells ascending by index value, in place.  An
    /// <see cref="H3Index"/> is a single <see cref="ulong"/> and its ordering is
    /// defined by that value (see <see cref="H3Index.CompareTo"/>), so on runtimes
    /// with the in-box primitive span sort the backing store is reinterpreted as
    /// <see cref="ulong"/> and sorted directly — bit-for-bit identical to
    /// <c>cells.Sort()</c> but without the per-comparison comparer dispatch and
    /// with no allocation.  netstandard (where perf is not measured and
    /// <c>MemoryExtensions.Sort</c> is not uniformly available) uses the list sort.
    /// </summary>
    private static void SortByValueAscending(List<H3Index> cells) {
#if NET8_0_OR_GREATER
        MemoryMarshal.Cast<H3Index, ulong>(CollectionsMarshal.AsSpan(cells)).Sort();
#else
        cells.Sort();
#endif
    }

    /// <summary>
    /// A minimal open-addressed, linear-probed set of the (non-zero)
    /// <see cref="H3Index"/> cell values already seen while deduplicating an
    /// <see cref="UncompactCells(IEnumerable{H3Index},int)"/> input set, backed by a <see cref="ulong"/>
    /// array rented from <see cref="ArrayPool{T}.Shared"/> (<see cref="H3Index"/>
    /// converts implicitly to/from <see cref="ulong"/>).  Cell values are never
    /// zero (<see cref="H3Index.Invalid"/> is filtered out before being added),
    /// so zero marks an empty slot.  This mirrors the probe tables used by the
    /// polyfill flood fill and grid-disk traversal and replaces a
    /// <see cref="HashSet{T}"/>: the single contiguous backing array both
    /// allocates far less than the hash set's bucket + entry arrays (and is
    /// returned to the pool) and probes with better cache locality on the fill's
    /// hottest operation — the per-input membership test.  The table doubles and
    /// rehashes if the presized capacity is exceeded, so membership semantics are
    /// identical to the hash set regardless of the estimate.
    /// </summary>
    private struct SeenCellSet {

        private ulong[]? _slots;
        private int _count;
        private int _growAt;

        public SeenCellSet(int capacity) {
            // grow the power-of-two table until `capacity` values fit under a
            // 0.75 load factor (capacity <= 0.75 * size, i.e. size >= capacity * 4 / 3)
            var size = 8;
            var desired = (long)capacity * 4 / 3 + 1;
            while (size < desired && size < (1 << 30)) size <<= 1;

            // Rent the backing table from the shared pool rather than allocating a
            // fresh array on every call.  The pool returns a power-of-two-length
            // array (>= size) whose contents are undefined, so clear it for the
            // empty-slot (0) sentinel and derive mask/growAt from the actual rented
            // Length, never the requested size.
            var slots = ArrayPool<ulong>.Shared.Rent(size);
            Array.Clear(slots, 0, slots.Length);
            _slots = slots;
            _count = 0;
            _growAt = slots.Length - (slots.Length >> 2);
        }

        /// <summary>
        /// Adds a (non-zero) value, returning false if it was already present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(ulong value) {
            if (_count >= _growAt) Grow();

            var slots = _slots!;
            var mask = slots.Length - 1;
            var slot = (int)((value * 0x9E3779B97F4A7C15UL) >> 32) & mask;
            while (true) {
                var existing = slots[slot];
                if (existing == 0) {
                    slots[slot] = value;
                    _count += 1;
                    return true;
                }

                if (existing == value) return false;
                slot = (slot + 1) & mask;
            }
        }

        private void Grow() {
            var old = _slots!;
            var newSize = old.Length << 1;
            if (newSize <= old.Length) return;   // table already at maximum size

            var slots = ArrayPool<ulong>.Shared.Rent(newSize);
            Array.Clear(slots, 0, slots.Length);
            var mask = slots.Length - 1;

            // all existing values are unique and non-zero, so reinsert without
            // duplicate checks
            foreach (var value in old) {
                if (value == 0) continue;
                var slot = (int)((value * 0x9E3779B97F4A7C15UL) >> 32) & mask;
                while (slots[slot] != 0) slot = (slot + 1) & mask;
                slots[slot] = value;
            }

            ArrayPool<ulong>.Shared.Return(old);
            _slots = slots;
            _growAt = slots.Length - (slots.Length >> 2);
        }

        /// <summary>
        /// Returns the rented backing table to the shared pool.  Idempotent, so it
        /// is safe to call from the iterator's <c>finally</c> on every completion
        /// or disposal.
        /// </summary>
        public void Dispose() {
            var slots = _slots;
            if (slots is null) return;
            _slots = null;
            ArrayPool<ulong>.Shared.Return(slots);
        }

    }

    /// <summary>
    /// A minimal append-and-sort list of <see cref="H3Index"/> backed by an
    /// array rented from <see cref="ArrayPool{T}"/>, used as the per-resolution
    /// compaction buckets in the span
    /// <see cref="CompactCells(ReadOnlySpan{H3Index},Span{H3Index})"/> so that
    /// compaction allocates nothing on a warm pool.  Grows by renting a larger
    /// array and copying, mirroring <see cref="SeenCellSet"/>.  Instances are
    /// stored in a plain array and mutated in place, so the value semantics of a
    /// struct are intentional — never copy an instance whose backing array is
    /// live.
    /// </summary>
    private struct PooledCellList {

        private const int InitialCapacity = 16;

        private H3Index[]? _array;
        private int _count;

        public readonly int Count => _count;

        public readonly H3Index this[int index] => _array![index];

        public void Add(H3Index value) {
            var array = _array;
            if (array == null) {
                array = ArrayPool<H3Index>.Shared.Rent(InitialCapacity);
                _array = array;
            } else if (_count == array.Length) {
                var grown = ArrayPool<H3Index>.Shared.Rent(array.Length << 1);
                Array.Copy(array, grown, _count);
                ArrayPool<H3Index>.Shared.Return(array);
                _array = array = grown;
            }

            array[_count++] = value;
        }

        /// <summary>
        /// Sorts the first <see cref="Count"/> cells ascending by index value,
        /// in place.  As with <see cref="SortByValueAscending"/> the backing store
        /// is reinterpreted as <see cref="ulong"/> and sorted with the in-box
        /// primitive span sort where available; netstandard uses the array sort.
        /// </summary>
        public readonly void Sort() {
            if (_count <= 1) return;
#if NET8_0_OR_GREATER
            MemoryMarshal.Cast<H3Index, ulong>(_array!.AsSpan(0, _count)).Sort();
#else
            Array.Sort(_array!, 0, _count);
#endif
        }

        public void Dispose() {
            var array = _array;
            if (array == null) return;
            _array = null;
            _count = 0;
            ArrayPool<H3Index>.Shared.Return(array);
        }

    }

}
