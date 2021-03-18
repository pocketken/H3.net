using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using H3.Algorithms;
using H3.Test;
using BenchmarkDotNet.Jobs;

namespace H3.Benchmarks.Algorithms {

    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class RingSlowVsRingFast {

        private static readonly H3Index TestIndex = TestHelpers.TestIndexValue;
        private const int K = 5;

        [Benchmark]
        public List<RingCell> GetKRingSlow() => TestIndex.GetKRingSlow(K).ToList();

        [Benchmark]
        public List<RingCell> GetKRingFast() => TestIndex.GetKRingFast(K).ToList();

    }

}
