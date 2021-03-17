using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using H3.Algorithms;
using H3.Test;

namespace H3.Benchmarks.Algorithms {

    public class RingSlowVsRingFast {

        private const int K = 5;

        [Benchmark]
        public List<RingCell> GetKRingSlow() => TestHelpers.SfIndex.GetKRingSlow(K).ToList();

        [Benchmark]
        public List<RingCell> GetKRingFast() => TestHelpers.SfIndex.GetKRingFast(K).ToList();

    }

}
