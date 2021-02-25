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
        /// <param name="indexes">set of hexagons</param>
        /// <returns>set of compressed hexagons</returns>
        public static IEnumerable<H3Index> Compact(this IEnumerable<H3Index> indexEnumerable) {
            List<H3Index> indexes = indexEnumerable.Distinct().ToList();

            if (!indexes.AreOfSameResolution()) {
                throw new ArgumentException("all indexes must be the same resolution");
            }

            int resolution = indexes[0].Resolution;

            // cant compress beyond res0
            if (resolution == 0) {
                foreach (var index in indexes) {
                    yield return index;
                }
                yield break;
            }

            // determine what can be compacted and what can't
            var (compactable, uncompactable) = GetCompactableParents(indexes);
            while (compactable.Any()) {
                // immediately return anything that can't be compacted further
                foreach (var index in uncompactable) {
                    yield return index;
                }

                // try and walk up and look for more
                (compactable, uncompactable) = GetCompactableParents(compactable);
            }

            foreach (var index in uncompactable) {
                yield return index;
            }
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
        public static IEnumerable<H3Index> UncompactToResolution(this IEnumerable<H3Index> indexes, int resolution) =>
            indexes.Where(index => index != H3Index.Invalid)
                .SelectMany(index => {
                    int currentResolution = index.Resolution;
                    if (!IsValidChildResolution(currentResolution, resolution)) {
                        throw new ArgumentException("set contains hexagon smaller than target resolution");
                    }

                    if (currentResolution == resolution) {
                        return index.ToEnumerable();
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
        public static bool AreOfSameResolution(this IEnumerable<H3Index> indexes) =>
            indexes.Select(index => index.Resolution).Distinct().Count() == 1;

        /// <summary>
        /// Splits the provided set of indexes into two separate enumerables -- Compactable,
        /// meaning the hexagon has a full set of children that can be pruned, and
        /// Uncompactable which are indexes that do not have a full set of children and
        /// therefore cannot be further compacted.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        private static (IEnumerable<H3Index>, IEnumerable<H3Index>) GetCompactableParents(IEnumerable<H3Index> indexes) {
            var byParent = indexes
                .Where(index => index.Resolution >= 1)
                .GroupBy(index => index.GetParentForResolution(index.Resolution - 1))
                .Select(g => (Parent: g.Key, Count: g.Count(), Indexes: new HashSet<H3Index>(g)))
                .Where(g => g.Count >= 7);

            var uncompactable = indexes.Where(i => !byParent.Any(e => e.Indexes.Contains(i)));
            var compactable = byParent.Select(e => e.Parent);

            return (compactable, uncompactable);
        }

    }
}
