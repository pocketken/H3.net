using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using H3.Test;
using H3Lib.Extensions;
using BenchmarkDotNet.Jobs;

namespace H3.Benchmarks.Algorithms {

    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class LineBenchmarks {

        private static readonly H3Index SfIndexAt14 = TestHelpers.SfIndex.GetChildCenterForResolution(14);
        private static readonly H3Lib.H3Index H3LibTestIndex = new(TestHelpers.TestIndexValue);
        private static readonly H3Lib.H3Index H3LibSfIndexAt14 = new(SfIndexAt14);

        [GlobalSetup]
        public void Setup() {
            Console.WriteLine($"TestIndexValue = {TestHelpers.TestIndexValue:x}");
            Console.WriteLine($"SfIndexAt14 = {SfIndexAt14}");
            Console.WriteLine($"length = {SfIndexAt14.DistanceTo(TestHelpers.TestIndexValue)}");
        }

        [Benchmark(Description = "pocketken.H3.LineTo")]
        public List<H3Index> LineTo() => SfIndexAt14.LineTo(TestHelpers.TestIndexValue).ToList();

        [Benchmark(Description = "H3Lib.LineTo")]
        public List<H3Lib.H3Index> LineToH3Lib() {
            var ret = H3LibSfIndexAt14.LineTo(H3LibTestIndex);
            return ret.Item2.ToList();
        }

    }

}
