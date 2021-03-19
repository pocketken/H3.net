using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using H3.Extensions;
using H3.Test;
using H3Lib.Extensions;

namespace H3.Benchmarks.Extensions {

    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [MemoryDiagnoser]
    public class HierarchyExtensionBenchmarks {

        private const int resolution = 15;
        private static readonly H3Lib.H3Index H3LibTestIndex = new(TestHelpers.SfIndex);

        [GlobalSetup]
        public void Setup() {
            Console.WriteLine($"SfIndex = {TestHelpers.SfIndex}");
        }

        [Benchmark(Description = "pocketken.H3.GetChildrenForResolution")]
        public List<H3Index> GetChildrenForResolution() => TestHelpers.SfIndex.GetChildrenForResolution(resolution).ToList();

        [Benchmark(Description = "H3Lib.ToChildren")]
        public List<H3Lib.H3Index> H3LibGetChildrenForResolution() => H3LibTestIndex.ToChildren(resolution).ToList();

    }
}
