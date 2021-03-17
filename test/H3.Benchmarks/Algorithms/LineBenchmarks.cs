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

namespace H3.Benchmarks.Algorithms {

    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class LineBenchmarks {

        private static readonly H3Index SfIndexAt14 = TestHelpers.SfIndex.GetChildCenterForResolution(14);

        [Benchmark]
        public int DistanceTo() => SfIndexAt14.DistanceTo(TestHelpers.TestIndexValue);

        [Benchmark]
        public List<H3Index> LineTo() => SfIndexAt14.LineTo(TestHelpers.TestIndexValue).ToList();
    }

}
