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
        /// Takes a set of hexagons all at the same resolution and compresses
        /// them by removing duplicates and pruning full child branches to the
        /// parent level. This is also done for all parents recursively to get
        /// the minimum number of hex addresses that perfectly cover the defined
        /// space.
        /// </summary>
        /// <remarks>This implementation differs from upstream in that duplicate
        /// or invalid inputs are filtered instead returning an error code when
        /// they are encountered.</remarks>
        /// <param name="indexEnumerable">set of hexagons to compress</param>
        /// <returns>set of compressed hexagons</returns>
        public static List<H3Index> Compact(this IEnumerable<H3Index> indexEnumerable) {
            List<H3Index> indexes = indexEnumerable
                .Where(index => index != H3Index.Invalid)
                .Distinct()
                .ToList();
            List<H3Index> results = new();

            if (!indexes.AreOfSameResolution()) {
                throw new ArgumentException("all indexes must be the same resolution");
            }

            int resolution = indexes[0].Resolution;

            // cant compress beyond res0
            if (resolution == 0) {
                return indexes;
            }

            // determine what can be compacted and what can't
            var compactable = GetCompactableParents(indexes, results);
            while (compactable.Count > 0) {
                // try and walk up and look for more
                compactable = GetCompactableParents(compactable, results);
            }

            // and return result
            return results;
        }

        /// <summary>
        /// Takes a compressed set of hexagons and expands back to the original
        /// set of hexagons at a specific resoution.
        /// </summary>
        /// <param name="indexes">set of hexagons</param>
        /// <param name="resolution">resolution to decompress to</param>
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
        /// Determines whether or not all H3Index entries within the array are
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

        /// <summary>
        /// Processes the provided set of indexes, returning hexagons identified as
        /// comptacted (meaning the hexagon had a full set of children that were
        /// pruned) and adding indexes that were not comptactable to the provided
        /// "uncompactable" index list.  This is the core of the compaction algorithm.
        /// </summary>
        /// <param name="indexes">Indexes to try and compact</param>
        /// <param name="uncompactable">List to add indexes identified as uncompactable
        /// to</param>
        /// <returns>List of parent indexes with children pruned</returns>
        private static List<H3Index> GetCompactableParents(List<H3Index> indexes, List<H3Index> uncompactable) {
            var byParent = indexes
                .Where(index => index.Resolution > 0)
                .GroupBy(index => index.GetParentForResolution(index.Resolution - 1))
                .Select(g => (Parent: g.Key, Indexes: g.ToList()))
                .Where(g => g.Indexes.Count == (g.Parent.IsPentagon ? 6 : 7));

            List<H3Index> compacted = new();
            HashSet<H3Index> compactedChildren = new();
            foreach (var (Parent, Indexes) in byParent) {
                compacted.Add(Parent);
                compactedChildren.UnionWith(Indexes);
            }

            uncompactable.AddRange(indexes.Where(index => !compactedChildren.Contains(index)));
            return compacted;
        }

    }
}
