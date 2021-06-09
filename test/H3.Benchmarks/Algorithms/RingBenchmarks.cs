using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using H3.Test;
using H3Lib.Extensions;
using BenchmarkDotNet.Jobs;
using H3.Model;

namespace H3.Benchmarks.Algorithms {

    [SimpleJob(RuntimeMoniker.Net50)]
    [MemoryDiagnoser]
    public class RingBenchmarks {

        private static readonly H3Index TestIndex = new H3Index(TestHelpers.TestIndexValue).GetChildCenterForResolution(15);
        private static readonly H3Lib.H3Index TestH3LibIndex = new(TestIndex);
        private static readonly H3Index TestPentagonIndex = LookupTables.PentagonIndexesPerResolution[14].First();
        private static readonly H3Lib.H3Index TestH3LibPentagonIndex = new(TestPentagonIndex);

        private const int K = 50;

        [GlobalSetup]
        public void Setup() {
            Console.WriteLine($"Hexagon = {TestIndex}");
            Console.WriteLine($"Pentagon = {TestPentagonIndex}");
        }

        [Benchmark(Description = "pocketken.H3.GetKRing(hex, k = 50)")]
        public List<RingCell> GetKRingHex() => TestIndex.GetKRing(K).ToList();

        [Benchmark(Description = "pocketken.H3.GetKRingFast(hex, k = 50)")]
        public List<RingCell> GetKRingHexFast() => TestIndex.GetKRingFast(K).ToList();

        [Benchmark(Description = "pocketken.H3.GetKRingSlow(hex, k = 50)")]
        public List<RingCell> GetKRingHexSlow() => TestPentagonIndex.GetKRingSlow(K).ToList();

        [Benchmark(Description = "pocketken.H3.GetKRing(pent, k = 50)")]
        public List<RingCell> GetKRingPent() => TestPentagonIndex.GetKRing(K).ToList();

        [Benchmark(Description = "pocketken.H3.GetKRingSlow(pent, k = 50)")]
        public List<RingCell> GetKRingPentSlow() => TestPentagonIndex.GetKRingSlow(K).ToList();

        [Benchmark(Description = "H3Lib.KRingDistances(hex, k = 50)")]
        public Dictionary<H3Lib.H3Index, int> H3Lib_KRingDistancesHex() => TestH3LibIndex.KRingDistances(K);

        [Benchmark(Description = "H3Lib.KRingDistances(pent, k = 50)")]
        public Dictionary<H3Lib.H3Index, int> H3Lib_KRingDistancesPent() => TestH3LibPentagonIndex.KRingDistances(K);
    }

}
