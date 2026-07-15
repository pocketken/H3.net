// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

// pocketken.H3's root namespace is "H3", which collides with the native binding's
// "H3.NET.Native.*" types. This class is pure pocketken (managed port, working tree
// via ProjectReference) so it aliases every pocketken type it touches, matching the
// aliasing style of the native-comparison classes.
using PocketkenIndex = H3.H3Index;
using PocketkenLatLng = H3.Model.LatLng;
using PocketkenRingCell = H3.Algorithms.RingCell;
using PocketkenRings = H3.Algorithms.Rings;
using PocketkenLines = H3.Algorithms.Lines;
using PocketkenHierarchy = H3.Extensions.H3HierarchyExtensions;
using PocketkenSet = H3.Extensions.H3SetExtensions;
using PocketkenGeometry = H3.Extensions.H3GeometryExtensions;

namespace H3.NET.Native.Benchmarks;

/// <summary>
/// pocketken.H3 A/B: for every operation that gained an additive zero-allocation
/// span / buffer-fill overload, the streaming <see cref="System.Collections.Generic.IEnumerable{T}"/>
/// method (baseline of its category) is measured against the span overload writing
/// into a buffer allocated once and reused across iterations.  The MemoryDiagnoser
/// "Allocated" column is the headline: the span rows allocate zero managed bytes per
/// call, the streaming rows the current cost.  Times are secondary.
///
/// Every buffer is sized with the library's own size function
/// (<see cref="PocketkenRings.MaxGridDiskSize"/>, <c>CellToChildrenSize</c>,
/// <c>GridPathCellsSize</c>, <c>UncompactCellsSize</c>, <see cref="PocketkenGeometry.MaxCellBoundaryVertices"/>)
/// so the destination is provably large enough.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public class SpanApiBenchmarks
{
    private const double Lat = 37.775938728915946;
    private const double Lng = -122.41795063018799;
    private const int OriginResolution = 9;
    private const int ChildResolution = 12;   // 7^3 = 343 children of a hexagon
    private const int K = 10;                  // gridDisk / gridRing radius

    private PocketkenIndex _origin;
    private PocketkenIndex _pathTarget;
    private PocketkenIndex[] _compactInput = [];
    private int _pathLength;

    // Buffers allocated once and reused every iteration — the intended zero-allocation
    // usage pattern (stackalloc for the small, bounded ones; a pooled/preallocated
    // field for the larger ones).
    private PocketkenIndex[] _childBuffer = [];
    private PocketkenRingCell[] _diskBuffer = [];
    private PocketkenIndex[] _ringBuffer = [];
    private PocketkenIndex[] _pathBuffer = [];
    private PocketkenLatLng[] _boundaryBuffer = [];
    private PocketkenIndex[] _compactBuffer = [];
    private PocketkenIndex[] _uncompactBuffer = [];

    // ---- native (H3.NET.Native) side: identical inputs, allocating + Into overloads ----
    private H3Index _nvOrigin;
    private H3Index _nvPathTarget;
    private H3Index[] _nvCompactInput = [];
    private H3Index[] _nvUncompactInput = [];
    private H3Index[] _nvChildBuf = [];
    private H3Index[] _nvDiskBuf = [];
    private int[] _nvDiskDistBuf = [];
    private H3Index[] _nvRingBuf = [];
    private H3Index[] _nvPathBuf = [];
    private H3Index[] _nvCompactBuf = [];
    private H3Index[] _nvUncompactBuf = [];

    [GlobalSetup]
    public void Setup()
    {
        _origin = PocketkenIndex.FromLatLng(PocketkenLatLng.FromCoordinate(new NetTopologySuite.Geometries.Coordinate(Lng, Lat)), OriginResolution);

        // a res-9 target ~K rings away for the grid path
        _pathTarget = PocketkenRings.GridDiskDistances(_origin, K).Last().Index;
        _pathLength = PocketkenLines.GridPathCellsSize(_origin, _pathTarget);

        // the full res-12 child set of the origin, which compacts back to [origin]
        _compactInput = PocketkenHierarchy.GetChildrenForResolution(_origin, ChildResolution).ToArray();

        _childBuffer = new PocketkenIndex[PocketkenHierarchy.CellToChildrenSize(_origin, ChildResolution)];
        _diskBuffer = new PocketkenRingCell[PocketkenRings.MaxGridDiskSize(K)];
        _ringBuffer = new PocketkenIndex[PocketkenRings.MaxGridRingSize(K)];
        _pathBuffer = new PocketkenIndex[_pathLength];
        _boundaryBuffer = new PocketkenLatLng[PocketkenGeometry.MaxCellBoundaryVertices];
        _compactBuffer = new PocketkenIndex[_compactInput.Length];
        _uncompactBuffer = new PocketkenIndex[PocketkenSet.UncompactCellsSize(new[] { _origin }, ChildResolution)];

        // native inputs mirror the pocketken ones by ulong value, so the per-op work is
        // identical; buffers are sized once for the zero-allocation Into overloads.
        _nvOrigin = new H3Index((ulong)_origin);
        _nvPathTarget = new H3Index((ulong)_pathTarget);
        _nvCompactInput = _compactInput.Select(c => new H3Index((ulong)c)).ToArray();
        _nvUncompactInput = new[] { _nvOrigin };

        var maxDisk = 3 * K * (K + 1) + 1;
        _nvChildBuf = new H3Index[_nvOrigin.CellToChildren(ChildResolution).Length];
        _nvDiskBuf = new H3Index[maxDisk];
        _nvDiskDistBuf = new int[maxDisk];
        _nvRingBuf = new H3Index[6 * K];
        _nvPathBuf = new H3Index[(int)_nvOrigin.GridDistance(_nvPathTarget) + 1];
        _nvCompactBuf = new H3Index[_nvCompactInput.Length];
        _nvUncompactBuf = new H3Index[H3Index.UncompactCells(_nvUncompactInput, ChildResolution).Length];
    }

    // ---- cellToChildren ----------------------------------------------------

    [BenchmarkCategory("GetChildren"), Benchmark(Baseline = true, Description = "streaming GetChildrenForResolution")]
    public int Children_Streaming() => PocketkenHierarchy.GetChildrenForResolution(_origin, ChildResolution).Count();

    [BenchmarkCategory("GetChildren"), Benchmark(Description = "span GetChildrenForResolution")]
    public int Children_Span() => PocketkenHierarchy.GetChildrenForResolution(_origin, ChildResolution, _childBuffer);

    // ---- gridDiskDistances -------------------------------------------------

    [BenchmarkCategory("GridDiskDistances"), Benchmark(Baseline = true, Description = "streaming GridDiskDistances")]
    public int Disk_Streaming() => PocketkenRings.GridDiskDistances(_origin, K).Count();

    [BenchmarkCategory("GridDiskDistances"), Benchmark(Description = "span GridDiskDistances")]
    public int Disk_Span() => PocketkenRings.GridDiskDistances(_origin, K, _diskBuffer);

    // ---- gridRingUnsafe ----------------------------------------------------

    [BenchmarkCategory("GridRingUnsafe"), Benchmark(Baseline = true, Description = "streaming GridRingUnsafe")]
    public int Ring_Streaming() => PocketkenRings.GridRingUnsafe(_origin, K).Count();

    [BenchmarkCategory("GridRingUnsafe"), Benchmark(Description = "span GridRingUnsafe")]
    public int Ring_Span() => PocketkenRings.GridRingUnsafe(_origin, K, _ringBuffer);

    // ---- gridPathCells -----------------------------------------------------

    [BenchmarkCategory("GridPathCells"), Benchmark(Baseline = true, Description = "streaming GridPathCells")]
    public int Path_Streaming() => PocketkenLines.GridPathCells(_origin, _pathTarget).Count();

    [BenchmarkCategory("GridPathCells"), Benchmark(Description = "span GridPathCells")]
    public int Path_Span() => PocketkenLines.GridPathCells(_origin, _pathTarget, _pathBuffer);

    // ---- cellToBoundary ----------------------------------------------------

    [BenchmarkCategory("CellBoundary"), Benchmark(Baseline = true, Description = "streaming GetCellBoundaryVertices")]
    public int Boundary_Streaming() => PocketkenGeometry.GetCellBoundaryVertices(_origin).Count();

    [BenchmarkCategory("CellBoundary"), Benchmark(Description = "span GetCellBoundaryVertices")]
    public int Boundary_Span() => PocketkenGeometry.GetCellBoundaryVertices(_origin, _boundaryBuffer);

    // ---- compactCells ------------------------------------------------------

    [BenchmarkCategory("CompactCells"), Benchmark(Baseline = true, Description = "streaming CompactCells")]
    public int Compact_Streaming() => PocketkenSet.CompactCells(_compactInput).Count;

    [BenchmarkCategory("CompactCells"), Benchmark(Description = "span CompactCells")]
    public int Compact_Span() => PocketkenSet.CompactCells(_compactInput, _compactBuffer);

    // ---- uncompactCells ----------------------------------------------------

    [BenchmarkCategory("UncompactCells"), Benchmark(Baseline = true, Description = "streaming UncompactCells")]
    public int Uncompact_Streaming() => PocketkenSet.UncompactCells((System.Collections.Generic.IEnumerable<PocketkenIndex>)new[] { _origin }, ChildResolution).Count();

    [BenchmarkCategory("UncompactCells"), Benchmark(Description = "span UncompactCells")]
    public int Uncompact_Span() => PocketkenSet.UncompactCells(new[] { _origin }, ChildResolution, _uncompactBuffer);

    // ---- native (H3.NET.Native) equivalents: allocating array + zero-alloc Into span ----

    [BenchmarkCategory("GetChildren"), Benchmark(Description = "H3.NET.Native CellToChildren (array)")]
    public int Nv_Children_Array() => _nvOrigin.CellToChildren(ChildResolution).Length;
    [BenchmarkCategory("GetChildren"), Benchmark(Description = "H3.NET.Native CellToChildrenInto (span)")]
    public int Nv_Children_Into() => _nvOrigin.CellToChildrenInto(ChildResolution, _nvChildBuf);

    [BenchmarkCategory("GridDiskDistances"), Benchmark(Description = "H3.NET.Native GridDiskDistances (array)")]
    public int Nv_Disk_Array() { var (c, _) = _nvOrigin.GridDiskDistances(K); return c.Length; }
    [BenchmarkCategory("GridDiskDistances"), Benchmark(Description = "H3.NET.Native GridDiskDistancesInto (span)")]
    public int Nv_Disk_Into() => _nvOrigin.GridDiskDistancesInto(K, _nvDiskBuf, _nvDiskDistBuf);

    // native GridRing is the pentagon-safe equivalent of pocketken's GridRingUnsafe.
    [BenchmarkCategory("GridRingUnsafe"), Benchmark(Description = "H3.NET.Native GridRing (array)")]
    public int Nv_Ring_Array() => _nvOrigin.GridRing(K).Length;
    [BenchmarkCategory("GridRingUnsafe"), Benchmark(Description = "H3.NET.Native GridRingInto (span)")]
    public int Nv_Ring_Into() => _nvOrigin.GridRingInto(K, _nvRingBuf);

    [BenchmarkCategory("GridPathCells"), Benchmark(Description = "H3.NET.Native GridPathCells (array)")]
    public int Nv_Path_Array() => _nvOrigin.GridPathCells(_nvPathTarget).Length;
    [BenchmarkCategory("GridPathCells"), Benchmark(Description = "H3.NET.Native GridPathCellsInto (span)")]
    public int Nv_Path_Into() => _nvOrigin.GridPathCellsInto(_nvPathTarget, _nvPathBuf);

    // native has only an allocating boundary accessor (no Into overload).
    [BenchmarkCategory("CellBoundary"), Benchmark(Description = "H3.NET.Native GetBoundary")]
    public int Nv_Boundary_Array() => _nvOrigin.GetBoundary().Count;

    [BenchmarkCategory("CompactCells"), Benchmark(Description = "H3.NET.Native CompactCells (array)")]
    public int Nv_Compact_Array() => H3Index.CompactCells(_nvCompactInput).Length;
    [BenchmarkCategory("CompactCells"), Benchmark(Description = "H3.NET.Native CompactCellsInto (span)")]
    public int Nv_Compact_Into() => H3Index.CompactCellsInto(_nvCompactInput, _nvCompactBuf);

    [BenchmarkCategory("UncompactCells"), Benchmark(Description = "H3.NET.Native UncompactCells (array)")]
    public int Nv_Uncompact_Array() => H3Index.UncompactCells(_nvUncompactInput, ChildResolution).Length;
    [BenchmarkCategory("UncompactCells"), Benchmark(Description = "H3.NET.Native UncompactCellsInto (span)")]
    public int Nv_Uncompact_Into() => H3Index.UncompactCellsInto(_nvUncompactInput, ChildResolution, _nvUncompactBuf);
}
