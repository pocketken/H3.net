using H3.Model;
using System.Collections.Generic;
using System.Linq;
using H3.Extensions;

namespace H3.Benchmarks;

internal class Common {
    // result of encoding Point(-110, 30) @ Res14 in PG
    public const ulong TestIndexValue = 0x8e48e1d7038d527;

    // uber sf test point
    public static readonly GeoCoord SfCoord = (0.659966917655, -2.1364398519396);
    public static readonly H3Index SfIndex = H3Index.FromGeoCoord(SfCoord, 9);

    /// <summary>
    /// Returns all of the resolution 0 base cell indexes.
    /// </summary>
    /// <returns></returns>
    public static readonly List<H3Index> AllResolution0Indexes =
        Enumerable.Range(0, Constants.NUM_BASE_CELLS)
            .Select(baseCellNumber => new H3Index
            {
                Mode = Mode.Cell,
                BaseCellNumber = baseCellNumber,
                Resolution = 0
            })
            .ToList();

    public static IEnumerable<H3Index> GetAllCellsForResolution(int resolution) {
        return resolution == 0 ? AllResolution0Indexes : AllResolution0Indexes.UncompactToResolution(resolution);
    }
}
