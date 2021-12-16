using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Collections.Generic;
using System.Linq;
using H3.Test;
using H3.Extensions;
using H3Lib.Extensions;

namespace H3.Benchmarks.Extensions {

    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class CompactBenchmarks {

        private const int Resolution = 5;

        private static readonly List<H3Index> TestCompactList = TestHelpers.GetAllCellsForResolution(Resolution).ToList();

        private static readonly List<H3Lib.H3Index> H3LibTestCompactList = TestCompactList
            .Select(i => new H3Lib.H3Index(i))
            .ToList();

        [Benchmark(Description = "pocketken.H3.Compact")]
        public List<H3Index> PocketkenCompact() => TestCompactList.Compact();

        [Benchmark(Description = "H3Lib.Compact")]
        public List<H3Lib.H3Index> H3LibCompact() => H3LibTestCompactList.Compact().Item2;
    }

    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class UncompactBenchmarks {

        private const int Resolution = 5;

        private static readonly List<H3Lib.H3Index> H3LibTestUncompact = TestHelpers.AllResolution0Indexes
            .Select(i => new H3Lib.H3Index(i))
            .ToList();

        [Benchmark(Description = "pocketken.H3.Uncompact")]
        public List<H3Index> Uncompact() => TestHelpers.AllResolution0Indexes.UncompactToResolution(Resolution).ToList();

        [Benchmark(Description = "H3Lib.Uncompact")]
        public List<H3Lib.H3Index> UncompactH3Lib() {
            var results = H3LibTestUncompact.Uncompact(Resolution);
            return results.Item2.ToList();
        }

    }

}
