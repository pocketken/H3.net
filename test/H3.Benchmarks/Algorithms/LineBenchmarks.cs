using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
#if RUN_COMPARATIVE_BENCHMARKS
using H3Lib.Extensions;
#endif

namespace H3.Benchmarks.Algorithms; 

[Config(typeof(CompareVersionsBenchmarkConfig))]
[MemoryDiagnoser]
public class LineBenchmarks {

    private static readonly H3Index SfIndexAt14 = Common.SfIndex.GetChildCenterForResolution(14);
#if RUN_COMPARATIVE_BENCHMARKS
    private static readonly H3Lib.H3Index H3LibTestIndex = new(Common.TestIndexValue);
    private static readonly H3Lib.H3Index H3LibSfIndexAt14 = new(SfIndexAt14);
#endif

    [GlobalSetup]
    public void Setup() {
        Console.WriteLine($"TestIndexValue = {Common.TestIndexValue:x}");
        Console.WriteLine($"SfIndexAt14 = {SfIndexAt14}");
        Console.WriteLine($"length = {SfIndexAt14.DistanceTo(Common.TestIndexValue)}");
    }

    [Benchmark(Description = "pocketken.H3.LineTo")]
    public List<H3Index> LineTo() => SfIndexAt14.LineTo(Common.TestIndexValue).ToList();

#if RUN_COMPARATIVE_BENCHMARKS
    [Benchmark(Description = "H3Lib.LineTo")]
    public List<H3Lib.H3Index> LineToH3Lib() {
        var ret = H3LibSfIndexAt14.LineTo(H3LibTestIndex);
        return ret.Item2.ToList();
    }
#endif

}