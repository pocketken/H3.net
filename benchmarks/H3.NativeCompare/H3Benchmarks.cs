// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using NetTopologySuite.Geometries;

// pocketken.H3's root namespace is "H3", which collides with this binding's
// "H3.NET.Native.*" types (both expose an H3Index). Aliasing the managed-port
// types keeps every reference unambiguous and makes the A/B obvious at the call site.
using PocketkenIndex = H3.H3Index;
using PocketkenLatLng = H3.Model.LatLng;
using PocketkenPolyfill = H3.Algorithms.Polyfill;
using PocketkenRings = H3.Algorithms.Rings;
using PocketkenRingCell = H3.Algorithms.RingCell;
using PocketkenVertexTestMode = H3.Algorithms.VertexTestMode;

namespace H3.NET.Native.Benchmarks;

/// <summary>
/// Three-way comparison of the same H3 workloads: a bare libh3 P/Invoke floor
/// (<see cref="RawH3Native"/>), this native binding (H3.NET.Native), and the
/// fully-managed pocketken.H3 4.0.0 port.
///
/// Baseline framing (BenchmarkDotNet baselines are per-category):
/// - LatLngToCell and GridDisk: the raw P/Invoke call is the baseline, so the
///   binding column shows its thin marshalling overhead over bare native and the
///   pocketken column shows the managed-port cost, both relative to the same floor.
/// - PolygonToCells: there is no fair raw baseline (its C entry needs a
///   GeoPolygon* whose marshalling would duplicate the binding), so the native
///   binding itself is the baseline and pocketken is measured against it.
/// Every category therefore has exactly one baseline and no fabricated numbers.
/// </summary>
// BenchmarkDotNet's default config already emits the GitHub-flavored Markdown
// report (BenchmarkDotNet.Artifacts/results/*-report-github.md), which is what we
// want to paste into docs, so no explicit MarkdownExporter is added here (doing so
// duplicates the default and triggers a config warning). The attributes below add
// the category column, group the summary by category, keep declaration order so
// each category's baseline prints first, and record allocations.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public class H3Benchmarks
{
    // San Francisco-ish point used for indexing. These are DEGREES; the binding
    // converts to radians at its boundary, the raw baseline converts explicitly,
    // and the pocketken side feeds them through an NTS Coordinate (degrees) so all
    // three index the identical cell (res-9 SF cell 08928308280fffff).
    private const double Lat = 37.775938728915946;
    private const double Lng = -122.41795063018799;

    // Mid resolution for indexing and gridDisk.
    private const int Resolution = 9;

    // gridDisk ring size.
    private const int K = 5;

    // A mid-res cell to expand with gridDisk (computed once in setup).
    private H3Index _nativeCell;

    // The same cell as a pocketken index, so GridDisk per-iteration work is just
    // the ring expansion (no re-indexing) and mirrors the native side exactly.
    private PocketkenIndex _pocketkenOrigin;

    // A small triangular polygon around the test point (degrees).
    private static readonly LatLng[] PolygonExterior =
    [
        new LatLng(37.813318999983238, -122.4089866999972145),
        new LatLng(37.7866302000007224, -122.3805436999997056),
        new LatLng(37.7198061999978478, -122.3544736999993603),
    ];

    private GeoPolygon _nativePolygon = null!;

    // The same triangle as an NTS Polygon for pocketken's polyfill, built once so
    // per-iteration work is just the fill, mirroring the native side.
    private Polygon _pocketkenPolygon = null!;

    [GlobalSetup]
    public void Setup()
    {
        _nativeCell = H3Index.FromLatLng(new LatLng(Lat, Lng), Resolution);
        _nativePolygon = new GeoPolygon(PolygonExterior);

        // FromCoordinate takes an NTS Coordinate (X=lng, Y=lat, DEGREES) and is the
        // pocketken entry that matches the binding's degree input; the bare ctor
        // expects RADIANS, so using it with degree values would index a different
        // cell. Verified: this yields the same 08928308280fffff cell as the binding.
        _pocketkenOrigin = PocketkenIndex.FromLatLng(
            PocketkenLatLng.FromCoordinate(new Coordinate(Lng, Lat)),
            Resolution);

        // NTS rings must be explicitly closed (first coord repeated); X=lng, Y=lat.
        var ring = new LinearRing(
        [
            new Coordinate(-122.4089866999972145, 37.813318999983238),
            new Coordinate(-122.3805436999997056, 37.7866302000007224),
            new Coordinate(-122.3544736999993603, 37.7198061999978478),
            new Coordinate(-122.4089866999972145, 37.813318999983238),
        ]);
        _pocketkenPolygon = new Polygon(ring);
    }

    // ----------------------------------------------------------------------
    // Point indexing: latLng -> cell
    // ----------------------------------------------------------------------

    // Baseline = the bare libh3 call: the floor that the binding's marshalling and
    // the managed port are both measured against.
    [BenchmarkCategory("LatLngToCell")]
    [Benchmark(Baseline = true, Description = "raw libh3 latLngToCell")]
    public ulong RawLatLngToCell() => RawH3Native.LatLngToCell(Lat, Lng, Resolution);

    [BenchmarkCategory("LatLngToCell")]
    [Benchmark(Description = "H3.NET.Native FromLatLng")]
    public ulong NativeLatLngToCell() =>
        H3Index.FromLatLng(new LatLng(Lat, Lng), Resolution).Value;

    [BenchmarkCategory("LatLngToCell")]
    [Benchmark(Description = "pocketken.H3 FromLatLng")]
    public ulong PocketkenLatLngToCell() =>
        PocketkenIndex.FromLatLng(PocketkenLatLng.FromCoordinate(new Coordinate(Lng, Lat)), Resolution);

    // ----------------------------------------------------------------------
    // gridDisk over a mid-res cell
    // ----------------------------------------------------------------------

    [BenchmarkCategory("GridDisk")]
    [Benchmark(Baseline = true, Description = "raw libh3 gridDisk")]
    public int RawGridDisk() => RawH3Native.GridDiskCount(_nativeCell.Value, K);

    [BenchmarkCategory("GridDisk")]
    [Benchmark(Description = "H3.NET.Native GridDisk")]
    public int NativeGridDisk() => _nativeCell.GridDisk(K).Length;

    // GridDiskDistances is the non-obsolete pocketken equivalent of the native
    // gridDisk (GetKRing is marked [Obsolete] as of pocketken 4.0); it returns the
    // same all-cells-within-distance-k set, so its count matches the native Length.
    [BenchmarkCategory("GridDisk")]
    [Benchmark(Description = "pocketken.H3 GridDiskDistances")]
    public int PocketkenGridDisk() => PocketkenRings.GridDiskDistances(_pocketkenOrigin, K).Count();

    // pocketken.H3's span overload into a reused stack buffer (sized to
    // MaxGridDiskSize(K)) — the managed port's zero-allocation disk expansion, the
    // A/B partner of PocketkenGridDisk above.
    [BenchmarkCategory("GridDisk")]
    [Benchmark(Description = "pocketken.H3 GridDiskDistances (span)")]
    public int PocketkenGridDiskInto()
    {
        Span<PocketkenRingCell> destination = stackalloc PocketkenRingCell[PocketkenRings.MaxGridDiskSize(K)];
        return PocketkenRings.GridDiskDistances(_pocketkenOrigin, K, destination);
    }

    // ----------------------------------------------------------------------
    // polygonToCells over a small polygon
    // ----------------------------------------------------------------------

    // No raw baseline here (GeoPolygon* marshalling would duplicate the binding),
    // so the native binding is the baseline and pocketken is measured against it.
    [BenchmarkCategory("PolygonToCells")]
    [Benchmark(Baseline = true, Description = "H3.NET.Native ToCells")]
    public int NativePolygonToCells() => H3Polygon.ToCells(_nativePolygon, Resolution).Length;

    // VertexTestMode.Center is passed explicitly (it is also Fill's default) so the
    // equivalence with the binding's CONTAINMENT_CENTER is pinned at the call site and
    // cannot silently drift if pocketken ever changes that default.
    [BenchmarkCategory("PolygonToCells")]
    [Benchmark(Description = "pocketken.H3 Polyfill.Fill")]
    public int PocketkenPolygonToCells() =>
        PocketkenPolyfill.Fill(_pocketkenPolygon, Resolution, PocketkenVertexTestMode.Center).Count();
}
