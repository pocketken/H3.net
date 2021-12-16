using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using H3.Model;

namespace H3.Benchmarks.Model {

    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class CoordIjkBenchmarks {

        private static readonly CoordIJK TestIjk = new (1, 4, -2);

        [Benchmark(Baseline = true)]
        public static CoordIJK NormalizeTest() => TestIjk.Normalize();

        [Benchmark]
        public static CoordIJK StaticNormalizeTest() => CoordIJK.Normalize(TestIjk);

    }

}