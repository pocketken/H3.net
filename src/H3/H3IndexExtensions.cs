using System;
using System.Collections.Generic;
using System.Linq;

namespace H3 {

	public static class H3IndexExtensions {

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
