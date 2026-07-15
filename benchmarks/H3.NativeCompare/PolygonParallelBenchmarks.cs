// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NetTopologySuite.Geometries;
using PocketkenPolyfill = H3.Algorithms.Polyfill;
using PocketkenVertexTestMode = H3.Algorithms.VertexTestMode;

namespace H3.NET.Native.Benchmarks;

/// <summary>
/// Large-fill polygon sweep pitting pocketken.H3's sequential <c>Fill</c> against its
/// multi-threaded <c>ParallelFill</c> and the native binding's <c>ToCells</c>, over the
/// same fixed ~0.5° SF box at the high resolutions (10–12) where a fill is big enough for
/// parallelism to pay for its overhead.  The baseline is sequential <c>Fill</c>, so the
/// ratio column reads directly as the parallel speedup and the native comparison.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.Declared)]
public class PolygonParallelBenchmarks
{
    private const double MinLat = 37.525, MaxLat = 38.025, MinLng = -122.668, MaxLng = -122.168;

    [Params(5, 6, 7, 8, 9, 10, 11, 12)]
    public int Resolution { get; set; }

    private Polygon _pocketkenBox = null!;
    private GeoPolygon _nativeBox = null!;

    [GlobalSetup]
    public void Setup()
    {
        _pocketkenBox = new Polygon(new LinearRing(new[]
        {
            new Coordinate(MinLng, MinLat),
            new Coordinate(MaxLng, MinLat),
            new Coordinate(MaxLng, MaxLat),
            new Coordinate(MinLng, MaxLat),
            new Coordinate(MinLng, MinLat),
        }));
        _nativeBox = new GeoPolygon(new[]
        {
            new LatLng(MinLat, MinLng),
            new LatLng(MinLat, MaxLng),
            new LatLng(MaxLat, MaxLng),
            new LatLng(MaxLat, MinLng),
        });
    }

    [Benchmark(Baseline = true, Description = "pocketken.H3 Fill (sequential)")]
    public int PocketkenFill() =>
        PocketkenPolyfill.Fill(_pocketkenBox, Resolution, PocketkenVertexTestMode.Center).Count();

    [Benchmark(Description = "pocketken.H3 ParallelFill")]
    public int PocketkenParallelFill() =>
        PocketkenPolyfill.ParallelFill(_pocketkenBox, Resolution, PocketkenVertexTestMode.Center).Count();

    [Benchmark(Description = "H3.NET.Native ToCells")]
    public int NativeToCells() => H3Polygon.ToCells(_nativeBox, Resolution).Length;
}
