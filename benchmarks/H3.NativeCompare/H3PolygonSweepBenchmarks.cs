// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using NetTopologySuite.Geometries;

// pocketken.H3's root namespace is "H3", which collides with this binding's
// "H3.NET.Native.*" types. Only the two managed-port entry points this sweep touches
// are aliased; the aliasing mirrors H3Benchmarks so the A/B is obvious at the call site.
using PocketkenPolyfill = H3.Algorithms.Polyfill;
using PocketkenVertexTestMode = H3.Algorithms.VertexTestMode;

namespace H3.NET.Native.Benchmarks;

/// <summary>
/// Latency-vs-output-size sweep for polygonToCells. A single fixed ~0.5 degree box
/// around San Francisco is filled at a spread of resolutions, so the emitted cell
/// count climbs from ~1e0 (res 4) to ~1e5 (res 10) while the polygon itself never
/// changes. Both this native binding and the fully-managed pocketken.H3 4.0.0 port
/// fill the identical box at the identical resolution per parameter, so the
/// native-vs-pocketken crossover (~130-740 cells) is captured on both sides of it.
///
/// <see cref="Setup"/> prints the real native cell count for each resolution so the
/// charts can plot latency against actual output size rather than the resolution index.
/// The native binding is the per-category baseline (there is no fair raw P/Invoke
/// floor for polygonToCells: its C entry needs a GeoPolygon* whose marshalling would
/// just duplicate the binding), matching H3Benchmarks.PolygonToCells.
/// </summary>
// Like H3Benchmarks, this relies on BenchmarkDotNet's default config, which already
// emits both the CSV and GitHub-flavored Markdown reports
// (BenchmarkDotNet.Artifacts/results/*-report.csv and *-report-github.md). Adding
// those exporters explicitly only duplicates the default and triggers a config warning.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public class H3PolygonSweepBenchmarks
{
    // A fixed ~0.5 degree box centered on the SF test point (degrees). Held constant
    // across the sweep so resolution alone drives the output cell count.
    private const double MinLat = 37.525;
    private const double MaxLat = 38.025;
    private const double MinLng = -122.668;
    private const double MaxLng = -122.168;

    // Box exterior for the binding (degrees). An H3 GeoLoop is implicitly closed, so
    // the four distinct corners are supplied without repeating the first, matching
    // how H3Benchmarks builds its native GeoPolygon.
    private static readonly LatLng[] BoxExterior =
    [
        new LatLng(MinLat, MinLng),
        new LatLng(MinLat, MaxLng),
        new LatLng(MaxLat, MaxLng),
        new LatLng(MaxLat, MinLng),
    ];

    // Resolutions chosen so the native output spans ~1e0 (res 4) to ~1e5 (res 10),
    // bracketing the ~130-740 cell native-vs-pocketken crossover. res 11 is
    // intentionally excluded: the same box exceeds ~1e6 cells there.
    [Params(4, 5, 6, 7, 8, 9, 10, 11, 12)]
    public int Resolution { get; set; }

    private GeoPolygon _nativePolygon = null!;
    private Polygon _pocketkenPolygon = null!;

    [GlobalSetup]
    public void Setup()
    {
        _nativePolygon = new GeoPolygon(BoxExterior);

        // NTS rings must be explicitly closed (first coord repeated); X=lng, Y=lat.
        var ring = new LinearRing(
        [
            new Coordinate(MinLng, MinLat),
            new Coordinate(MaxLng, MinLat),
            new Coordinate(MaxLng, MaxLat),
            new Coordinate(MinLng, MaxLat),
            new Coordinate(MinLng, MinLat),
        ]);
        _pocketkenPolygon = new Polygon(ring);

        // GlobalSetup runs once per parameter value, so this records the real native
        // output size for each resolution (the charts plot latency vs this count).
        int nativeCellCount = H3Polygon.ToCells(_nativePolygon, Resolution).Length;
        Console.WriteLine($"[PolygonSweep] res={Resolution} nativeCells={nativeCellCount}");
    }

    [BenchmarkCategory("PolygonSweep")]
    [Benchmark(Baseline = true, Description = "H3.NET.Native ToCells")]
    public int NativePolygonSweep() => H3Polygon.ToCells(_nativePolygon, Resolution).Length;

    // VertexTestMode.Center is passed explicitly (it is also Fill's default) so the
    // equivalence with the binding's CONTAINMENT_CENTER is pinned at the call site,
    // matching H3Benchmarks.PocketkenPolygonToCells.
    [BenchmarkCategory("PolygonSweep")]
    [Benchmark(Description = "pocketken.H3 Polyfill.Fill")]
    public int PocketkenPolygonSweep() =>
        PocketkenPolyfill.Fill(_pocketkenPolygon, Resolution, PocketkenVertexTestMode.Center).Count();
}
