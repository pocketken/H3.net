using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using H3.Algorithms;
using H3.Extensions;
using H3.Test;
using BenchmarkDotNet.Jobs;
using H3Lib.Extensions;

namespace H3.Benchmarks.Algorithms {

    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [MemoryDiagnoser]
    public class LineBenchmarks {

        private static readonly H3Index SfIndexAt14 = TestHelpers.SfIndex.GetChildCenterForResolution(14);
        private static readonly H3Lib.H3Index H3LibTestIndex = new(TestHelpers.TestIndexValue);
        private static readonly H3Lib.H3Index H3LibSfIndexAt14 = new(SfIndexAt14);

        [Benchmark(Baseline = true, Description = "pocketken.H3.LineTo")]
        public List<H3Index> LineTo() => SfIndexAt14.LineTo(TestHelpers.TestIndexValue).ToList();

        [Benchmark(Description = "H3Lib.LineTo")]
        public List<H3Lib.H3Index> LineToH3Lib() {
            var ret = H3LibSfIndexAt14.LineTo(H3LibTestIndex);
            return ret.Item2.ToList();
        }

    }

}
