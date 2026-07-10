using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Linq;
using H3.Extensions;

namespace H3.Benchmarks.Extensions;

[Config(typeof(CompareVersionsBenchmarkConfig))]
[MemoryDiagnoser]
public class CompactBenchmarks {

    private const int RESOLUTION = 5;

    private static readonly List<H3Index> TestCompactList = Common.GetAllCellsForResolution(RESOLUTION).ToList();

    private static readonly List<H3Lib.H3Index> H3LibTestCompactList = TestCompactList
        .Select(i => new H3Lib.H3Index(i))
        .ToList();

    [Benchmark(Description = "pocketken.H3.Compact")]
    public List<H3Index> PocketkenCompact() => TestCompactList.CompactCells();

    // [Benchmark(Description = "H3Lib.Compact")]
    // public List<H3Lib.H3Index> H3LibCompact() => H3LibTestCompactList.Compact().Item2;
}

[Config(typeof(CompareVersionsBenchmarkConfig))]
[MemoryDiagnoser]
public class UncompactBenchmarks {

    private const int RESOLUTION = 5;

    private static readonly List<H3Lib.H3Index> H3LibTestUncompact = Common.AllResolution0Indexes
        .Select(i => new H3Lib.H3Index(i))
        .ToList();

    [Benchmark(Description = "pocketken.H3.Uncompact")]
    public List<H3Index> Uncompact() => Common.AllResolution0Indexes.UncompactCells(RESOLUTION).ToList();

    // [Benchmark(Description = "H3Lib.Uncompact")]
    // public List<H3Lib.H3Index> UncompactH3Lib() {
    //     var results = H3LibTestUncompact.Uncompact(Resolution);
    //     return results.Item2.ToList();
    // }

}