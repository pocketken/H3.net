using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Linq;
using H3.Test;
using H3.Model;

namespace H3.Benchmarks {

    [SimpleJob(RuntimeMoniker.Net50)]
    [MemoryDiagnoser]
    public class H3IndexBenchmarks {

        private static readonly H3Index TestIndex = TestHelpers.TestIndexValue;
        private static readonly H3Index PentagonIndex = LookupTables.PentagonIndexesPerResolution[14].First();

        [Benchmark(Description = "pocketken.H3.H3Index.RotateClockwise(hex)")]
        public void PocketkenRotateHexClockwise() => TestIndex.RotateClockwise();

        [Benchmark(Description = "pocketken.H3.H3Index.RotateCounterClockwise(hex)")]
        public void PocketkenRotateHexCounterClockwise() => TestIndex.RotateCounterClockwise();

        [Benchmark(Description = "pocketken.H3.H3Index.RotateClockwise(pent)")]
        public void PocketkenRotatePentagonClockwise() => PentagonIndex.RotateClockwise();

        [Benchmark(Description = "pocketken.H3.H3Index.RotateCounterClockwise(pent)")]
        public void PocketkenRotatePentagonCounterClockwise() => PentagonIndex.RotateCounterClockwise();

    }
}
