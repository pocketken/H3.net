using System;
using System.Collections.Generic;
using System.Linq;
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

        // first group by resolution
        foreach (var index in indexEnumerable) {
            if (index == H3Index.Invalid) {
                continue;
            }

            var indexResolution = index.Resolution;
            if (indexResolution > maxResolution) maxResolution = indexResolution;

            (byResolution[indexResolution] ??= new List<H3Index>()).Add(index);
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

            toCompact.Sort();
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
            zeroes.Sort();
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
    public static IEnumerable<H3Index> UncompactCells(this IEnumerable<H3Index> indexes, int resolution) =>
        indexes.Where(index => index != H3Index.Invalid)
            .Distinct()
            .SelectMany(index => {
                var currentResolution = index.Resolution;
                if (!IsValidChildResolution(currentResolution, resolution)) {
                    throw new ArgumentException("set contains cell smaller than target resolution");
                }

                return index.GetChildrenForResolution(resolution);
            });

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
    /// the output of <see cref="CompactCells"/></param>
    /// <returns>canonicalized set of cells</returns>
    public static List<H3Index> CanonicalizeCells(this IEnumerable<H3Index> cells) {
        List<H3Index> result = cells is IReadOnlyCollection<H3Index> collection ? new(collection.Count) : new();

        foreach (var cell in cells) {
            if (cell != H3Index.Invalid) result.Add(cell);
        }

        result.Sort();

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
    /// the output of <see cref="CompactCells"/></param>
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

}
