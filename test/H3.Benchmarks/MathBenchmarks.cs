using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;

namespace H3.Benchmarks {

    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class MathBenchmarks {

        private const double Value = 0.2387273897282832;

        [Benchmark(Description="Math.Round", Baseline = true)]
        public static int MathRound() => (int)Math.Round(Value);

        [Benchmark(Description = "Math.Round with midpoint away from zero")]
        public static int MathRoundMidpoint() => (int)Math.Round(Value, MidpointRounding.AwayFromZero);

        [Benchmark(Description = "Math.Floor + 0.5")]
        public static int MathFloor() => (int)Math.Floor(Value + 0.5);

    }

}