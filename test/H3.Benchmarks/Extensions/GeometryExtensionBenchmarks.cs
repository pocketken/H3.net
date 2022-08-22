using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Linq;
using H3.Extensions;
using H3.Model;
using H3Lib;
using H3Lib.Extensions;
using NetTopologySuite.Geometries;

namespace H3.Benchmarks.Extensions; 

[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net50)]
[SimpleJob(RuntimeMoniker.NetCoreApp31)]
[SimpleJob(RuntimeMoniker.Net48)]
[MemoryDiagnoser]
public class GeometryExtensionBenchmarks {

    private static readonly H3Index PentIndex = LookupTables.PentagonIndexesPerResolution[14].First();
    private static readonly H3Lib.H3Index H3LibTestIndex = new(Common.SfIndex);
    private static readonly H3Lib.H3Index H3LibPentIndex = new(PentIndex);
    private static readonly H3Index[] TestIndexChildren = Common.SfIndex.GetChildrenForResolution(15).ToArray();
    private static readonly H3Index[] PentIndexChildren = PentIndex.GetChildrenForResolution(15).ToArray();

    [GlobalSetup]
    public void Setup() {
        Console.WriteLine($"Hex = {Common.SfIndex}");
        Console.WriteLine($"Hex Children @ 15 = {TestIndexChildren.Length} cells");
        Console.WriteLine($"Pent = {PentIndex}");
        Console.WriteLine($"Pent Children @ 15 = {PentIndexChildren.Length} cells");
    }

    [Benchmark(Description = "pocketken.H3.H3GeometryExtensions.GetCellBoundary(hex)")]
    public Polygon PocketkenGetCellBoundaryHex() => Common.SfIndex.GetCellBoundary();

    [Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.ToGeoBoundary(hex)")]
    public GeoBoundary H3LibToGeoBoundaryHex() => H3LibTestIndex.ToGeoBoundary();

    [Benchmark(Description = "pocketken.H3.H3GeometryExtensions.GetCellBoundary(pent)")]
    public Polygon PocketkenGetCellBoundaryPent() => PentIndex.GetCellBoundary();

    [Benchmark(Description = "H3Lib.Extensions.H3IndexExtensions.ToGeoBoundary(pent)")]
    public GeoBoundary H3LibToGeoBoundaryPent() => H3LibPentIndex.ToGeoBoundary();

    [Benchmark(Description = "pocketken.H3.H3GeometryExtensions.GetCellBoundaries(hexChildren)")]
    public MultiPolygon PocketkenGetCellBoundariesHex() => TestIndexChildren.GetCellBoundaries();

    [Benchmark(Description = "pocketken.H3.H3GeometryExtensions.GetCellBoundaries(pentChildren)")]
    public MultiPolygon PocketkenGetCellBoundariesPent() => PentIndexChildren.GetCellBoundaries();

}