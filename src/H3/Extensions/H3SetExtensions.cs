using System;
using System.Collections.Generic;
using System.Linq;
using static H3.Utils;

namespace H3.Extensions {

    /// <summary>
    /// Provides extension methods that operate on sets of H3Index.
    /// </summary>
    public static class H3SetExtensions {

        /// <summary>
        /// Takes a set of hexagons and compacts them by removing duplicates and
        /// pruning full child branches to the parent level. This is also done for
        /// all parents recursively to get the minimum number of indexes that perfectly
        /// cover the defined space.</summary>
        /// <remarks>This implementation differs from upstream in that mixed resolutions
        /// are supported, and duplicate or invalid inputs are filtered instead returning
        /// an error code when they are encountered.  Based on the "FlexiCompact" method
        /// in H3Lib
        /// (https://github.com/RichardVasquez/h3net/blob/v3.7.1/H3Lib/Extensions/H3LibExtensions.cs#L359)
        /// </remarks>
        /// <param name="indexEnumerable">set of hexagons to compact</param>
        /// <returns>set of compacted hexagons</returns>
        public static List<H3Index> Compact(this IEnumerable<H3Index> indexEnumerable) {
            Dictionary<int, HashSet<H3Index>> indexes = new();
            int maxResolution = -1;
            int count = 0;

            // first group by resolution
            foreach (var index in indexEnumerable) {
                if (index == H3Index.Invalid) {
                    continue;
                }

                int indexResolution = index.Resolution;
                maxResolution = Math.Max(maxResolution, indexResolution);

                if (!indexes.ContainsKey(indexResolution)) {
                    indexes[indexResolution] = new HashSet<H3Index>();
                }

                indexes[indexResolution].Add(index);
                count++;
            }

            // worst case, nothing gets compacted
            List<H3Index> results = new(count);
            Dictionary<H3Index, List<H3Index>> parents = new();

            // loop backward through each resolution, throwing any compacted parents into
            // the resolution below us
            for (int resolution = maxResolution; resolution > 0; resolution -= 1) {
                if (indexes.TryGetValue(resolution, out var toCompact)) {
                    int parentResolution = resolution - 1;

                    foreach (var index in toCompact) {
                        var parent = index.GetParentForResolution(parentResolution);

                        if (!parents.ContainsKey(parent)) {
                            parents[parent] = new List<H3Index>(7);
                        }

                        parents[parent].Add(index);
                    }

                    // any parent that has enough children should be added
                    // back in to be tested at the next lowest resolution.
                    // anything else is uncompactable.
                    foreach (var (parent, children) in parents) {
                        if (children.Count >= (parent.IsPentagon ? 6 : 7)) {
                            if (!indexes.ContainsKey(parentResolution)) {
                                indexes[parentResolution] = new HashSet<H3Index>();
                            }
                            indexes[parentResolution].Add(parent);
                        } else {
                            results.AddRange(children);
                        }
                    }

                    if (resolution > 1) {
                        parents.Clear();
                    }
                }
            }

            // and lastly, add in any res 0
            if (indexes.TryGetValue(0, out var zeroes)) {
                results.AddRange(zeroes);
            }

            return results;
        }

        /// <summary>
        /// Takes a compacted set of hexagons and expands back to the original
        /// set of hexagons at a specific resoution.
        /// </summary>
        /// <param name="indexes">set of hexagons</param>
        /// <param name="resolution">resolution to decompact to</param>
        /// <returns>original set of hexagons. Thows ArgumentException if any
        /// hexagon in the set is smaller than the output resolution or invalid
        /// resolution is requested.</returns>
        public static IEnumerable<H3Index> UncompactToResolution(this IEnumerable<H3Index> indexes, int resolution) =>
            indexes.Where(index => index != H3Index.Invalid)
                .Distinct()
                .SelectMany(index => {
                    int currentResolution = index.Resolution;
                    if (!IsValidChildResolution(currentResolution, resolution)) {
                        throw new ArgumentException("set contains hexagon smaller than target resolution");
                    }

                    return index.GetChildrenForResolution(resolution);
                });

        /// <summary>
        /// Takes a set of indexes and expands to the highest found resolution
        /// within the set.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns>expanded set ofindexes</returns>
        public static IEnumerable<H3Index> UncompactToHighestResolution(this IEnumerable<H3Index> indexes) =>
            UncompactToResolution(indexes, indexes.Max(i => i.Resolution));

        /// <summary>
        /// Determines whether or not all H3Index entries within the enumerable are
        /// of the same resolution.
        /// </summary>
        /// <param name="indexes">set of hexagons</param>
        /// <returns>true if all hexagons are of the same resolution, false if
        /// not.
        /// </returns>
        public static bool AreOfSameResolution(this IEnumerable<H3Index> indexes) {
            int resolution = -1;
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
}
