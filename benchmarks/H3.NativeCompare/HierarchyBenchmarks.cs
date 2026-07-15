// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

// pocketken.H3's root namespace is "H3", which collides with this binding's
// "H3.NET.Native.*" types (both expose an H3Index). Aliasing the managed-port index
// and its hierarchy extensions keeps every reference unambiguous and makes the A/B
// obvious at the call site, mirroring the aliasing style in H3Benchmarks.
using PocketkenHierarchy = H3.Extensions.H3HierarchyExtensions;
using PocketkenIndex = H3.H3Index;

namespace H3.NET.Native.Benchmarks;

/// <summary>
/// Cell-hierarchy comparison (cellToParent and cellToChildren) between this native
/// binding (H3.NET.Native) and the fully-managed pocketken.H3 4.0.0 port, plus the
/// binding's own allocating vs span (zero-allocation) children overloads.
///
/// Fixed setup (printed by <see cref="Setup"/> for verification): the res-8 origin is
/// the SF test point; its k=6 gridDisk is sorted ascending by ulong value and the
/// first 100 cells are kept as the cellToParent inputs. Their unique res-7 parents are
/// the cellToChildren inputs, so both operations run over a stable, documented cell set.
///
/// Baselines are per-category (BenchmarkDotNet convention): the binding is the baseline
/// in each category, so the pocketken column shows the managed-port cost relative to it
/// and, for children, the span overload shows the allocation it removes.
/// </summary>
// Like H3Benchmarks, this relies on BenchmarkDotNet's default config, which already
// emits the GitHub-flavored Markdown report (BenchmarkDotNet.Artifacts/results/
// *-report-github.md). Adding those exporters explicitly only duplicates the default
// and triggers a config warning. The attributes add the category column, group the
// summary by category, keep declaration order so each baseline prints first, and
// record allocations.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public class HierarchyBenchmarks
{
    // San Francisco-ish point (DEGREES); the binding converts to radians at its
    // boundary. Same point the other benchmark classes index, here at res 8.
    private const double Lat = 37.775938728915946;
    private const double Lng = -122.41795063018799;

    // Origin resolution, gridDisk ring size, and the number of sorted disk cells kept.
    private const int OriginResolution = 8;
    private const int K = 6;
    private const int SourceCellCount = 100;

    // Hierarchy targets: coarser parent and (one level) finer child resolutions.
    private const int ParentResolution = 7;
    private const int ChildResolution = 8;

    // cellToChildrenSize for a single-level subdivision is 7 (the hexagon maximum;
    // pentagon parents simply fill fewer slots). Sizing the reused span to this lets
    // CellToChildrenInto write every parent's children with zero managed allocation.
    private const int MaxChildrenPerLevel = 7;

    // 100 res-8 source cells (binding) and their pocketken equivalents, built from the
    // identical ulong values so the per-iteration work is purely the hierarchy call.
    private H3Index[] _sourceCells = [];
    private PocketkenIndex[] _pocketkenSourceCells = [];

    // Unique res-7 parents of the 100 source cells (binding + pocketken).
    private H3Index[] _uniqueParents = [];
    private PocketkenIndex[] _pocketkenParents = [];

    [GlobalSetup]
    public void Setup()
    {
        var origin = H3Index.FromLatLng(new LatLng(Lat, Lng), OriginResolution);

        // Sort the k=6 disk ascending by ulong value and keep the first 100 cells.
        _sourceCells = origin.GridDisk(K)
            .OrderBy(cell => cell.Value)
            .Take(SourceCellCount)
            .ToArray();

        _pocketkenSourceCells = _sourceCells
            .Select(cell => new PocketkenIndex(cell.Value))
            .ToArray();

        // Distinct res-7 parents of the 100 source cells (H3Index has value equality).
        _uniqueParents = _sourceCells
            .Select(cell => cell.CellToParent(ParentResolution))
            .Distinct()
            .ToArray();

        _pocketkenParents = _uniqueParents
            .Select(parent => new PocketkenIndex(parent.Value))
            .ToArray();

        // Total res-8 children across the unique parents (verification only).
        var totalChildren = 0;
        foreach (var parent in _uniqueParents)
        {
            totalChildren += parent.CellToChildren(ChildResolution).Length;
        }

        Console.WriteLine(
            $"[Hierarchy] sourceCells={_sourceCells.Length} uniqueParents={_uniqueParents.Length} totalChildren={totalChildren}");
    }

    // ----------------------------------------------------------------------
    // cellToParent over the 100 source cells
    // ----------------------------------------------------------------------

    // XOR of the parent values is returned so nothing is dead-code-eliminated.
    [BenchmarkCategory("CellToParent")]
    [Benchmark(Baseline = true, Description = "H3.NET.Native CellToParent")]
    public ulong NativeCellToParent()
    {
        ulong aggregate = 0;
        foreach (var cell in _sourceCells)
        {
            aggregate ^= cell.CellToParent(ParentResolution).Value;
        }

        return aggregate;
    }

    [BenchmarkCategory("CellToParent")]
    [Benchmark(Description = "pocketken.H3 GetParentForResolution")]
    public ulong PocketkenCellToParent()
    {
        ulong aggregate = 0;
        foreach (var cell in _pocketkenSourceCells)
        {
            aggregate ^= PocketkenHierarchy.GetParentForResolution(cell, ParentResolution);
        }

        return aggregate;
    }

    // ----------------------------------------------------------------------
    // cellToChildren over the unique res-7 parents
    // ----------------------------------------------------------------------

    // Allocating overload: a fresh array per parent. Baseline of this category.
    [BenchmarkCategory("CellToChildren")]
    [Benchmark(Baseline = true, Description = "H3.NET.Native CellToChildren (array)")]
    public int NativeCellToChildren()
    {
        var total = 0;
        foreach (var parent in _uniqueParents)
        {
            total += parent.CellToChildren(ChildResolution).Length;
        }

        return total;
    }

    // Span overload into a single reused stack buffer: demonstrates the zero managed
    // bytes the allocating overload spends. The buffer is allocated once outside the
    // loop, so every parent's children are written into the same 7-element span.
    [BenchmarkCategory("CellToChildren")]
    [Benchmark(Description = "H3.NET.Native CellToChildrenInto (span)")]
    public int NativeCellToChildrenInto()
    {
        Span<H3Index> destination = stackalloc H3Index[MaxChildrenPerLevel];
        var total = 0;
        foreach (var parent in _uniqueParents)
        {
            total += parent.CellToChildrenInto(ChildResolution, destination);
        }

        return total;
    }

    [BenchmarkCategory("CellToChildren")]
    [Benchmark(Description = "pocketken.H3 GetChildrenForResolution")]
    public int PocketkenCellToChildren()
    {
        var total = 0;
        foreach (var parent in _pocketkenParents)
        {
            total += PocketkenHierarchy.GetChildrenForResolution(parent, ChildResolution).Count();
        }

        return total;
    }

    // pocketken.H3's span overload into a single reused stack buffer — the managed
    // port's equivalent of NativeCellToChildrenInto, writing every parent's children
    // into the same 7-element span with zero managed allocation.
    [BenchmarkCategory("CellToChildren")]
    [Benchmark(Description = "pocketken.H3 GetChildrenForResolution (span)")]
    public int PocketkenCellToChildrenInto()
    {
        Span<PocketkenIndex> destination = stackalloc PocketkenIndex[MaxChildrenPerLevel];
        var total = 0;
        foreach (var parent in _pocketkenParents)
        {
            total += PocketkenHierarchy.GetChildrenForResolution(parent, ChildResolution, destination);
        }

        return total;
    }
}
