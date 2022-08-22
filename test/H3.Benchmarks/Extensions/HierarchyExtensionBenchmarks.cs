using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using H3.Extensions;
using H3.Model;
using H3Lib.Extensions;

namespace H3.Benchmarks.Extensions; 

[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net50)]
[SimpleJob(RuntimeMoniker.NetCoreApp31)]
[SimpleJob(RuntimeMoniker.Net48)]
[MemoryDiagnoser]
public class HierarchyExtensionBenchmarks {

    private const int resolution = 15;
    private static readonly H3Index PentIndex = LookupTables.PentagonIndexesPerResolution[14].First();
    private static readonly H3Lib.H3Index H3LibTestIndex = new(Common.SfIndex);
    private static readonly H3Lib.H3Index H3LibPentIndex = new(PentIndex);

    [GlobalSetup]
    public void Setup() {
        Console.WriteLine($"Hex = {Common.SfIndex}");
        Console.WriteLine($"Pent = {PentIndex}");
    }

    [Benchmark(Description = "pocketken.H3.GetDirectNeighbour(hex, I)")]
    public (H3Index, int) GetDirectNeighbourHexI() => Common.SfIndex.GetDirectNeighbour(Direction.I);

    [Benchmark(Description = "pocketken.H3.GetDirectNeighbour(hex, IJ)")]
    public (H3Index, int) GetDirectNeighbourHexIJ() => Common.SfIndex.GetDirectNeighbour(Direction.IJ);

    [Benchmark(Description = "H3Lib.NeighborRotations(hex, I)")]
    public (H3Lib.H3Index, int) H3LibNeighborRotationsHexI() => H3LibTestIndex.NeighborRotations(H3Lib.Direction.I_AXES_DIGIT, 0);

    [Benchmark(Description = "H3Lib.NeighborRotations(hex, IJ)")]
    public (H3Lib.H3Index, int) H3LibNeighborRotationsHexIJ() => H3LibTestIndex.NeighborRotations(H3Lib.Direction.IJ_AXES_DIGIT, 0);

    [Benchmark(Description = "pocketken.H3.GetDirectNeighbour(pent, I)")]
    public (H3Index, int) GetDirectNeighbourPentI() => PentIndex.GetDirectNeighbour(Direction.I);

    [Benchmark(Description = "pocketken.H3.GetDirectNeighbour(pent, IJ)")]
    public (H3Index, int) GetDirectNeighbourPentIJ() => PentIndex.GetDirectNeighbour(Direction.IJ);

    [Benchmark(Description = "H3Lib.NeighborRotations(pent, I)")]
    public (H3Lib.H3Index, int) H3LibNeighborRotationsPentI() => H3LibPentIndex.NeighborRotations(H3Lib.Direction.I_AXES_DIGIT, 0);

    [Benchmark(Description = "H3Lib.NeighborRotations(pent, IJ)")]
    public (H3Lib.H3Index, int) H3LibNeighborRotationsPentIJ() => H3LibPentIndex.NeighborRotations(H3Lib.Direction.IJ_AXES_DIGIT, 0);

    [Benchmark(Description = "pocketken.H3.GetChildrenForResolution")]
    public List<H3Index> GetChildrenForResolution() => Common.SfIndex.GetChildrenForResolution(resolution).ToList();

    [Benchmark(Description = "H3Lib.ToChildren")]
    public List<H3Lib.H3Index> H3LibGetChildrenForResolution() => H3LibTestIndex.ToChildren(resolution).ToList();

    [Benchmark(Description = "pocketken.H3.GetParentForResolution")]
    public H3Index GetParentForResolution() => Common.SfIndex.GetParentForResolution(0);

    [Benchmark(Description = "H3Lib.ToParent")]
    public H3Lib.H3Index H3LibGetParentForResolution() => H3LibTestIndex.ToParent(0);

}