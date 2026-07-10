using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using H3;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using NetTopologySuite.Geometries;

namespace H3.CompareBenchmarks;

public class CompareVersionsConfig : ManualConfig {

    public CompareVersionsConfig() {
#if H3_400
        AddJob(Job.ShortRun.WithRuntime(CoreRuntime.Core80).WithId("4.0.0"));
#else
        AddJob(Job.ShortRun.WithRuntime(CoreRuntime.Core80).WithId("current"));
#endif
    }

}

[Config(typeof(CompareVersionsConfig))]
[MemoryDiagnoser]
public class CompareBenchmarks {

    private static readonly H3Index SfIndex = 0x8928308280fffff;
    private static readonly LatLng SfCoordinate = (0.659966917655, -2.1364398519396);
    private static readonly H3Index PathDestination = SfIndex.GridDiskDistances(20).Last().Index;
    private static readonly H3Index EdgeIndex = SfIndex.ToDirectedEdge(SfIndex.GetNeighbours().Skip(1).First());
    private static readonly List<H3Index> DiskCells = SfIndex.GridDiskDistances(9).Select(cell => cell.Index).ToList();
    private static readonly List<H3Index> CompactedCells = DiskCells.CompactCells();

    private static readonly Polygon SfPolygon = DefaultSfPolygon();

    private static Polygon DefaultSfPolygon() {
        LatLng[] vertices = {
            (0.659966917655, -2.1364398519396),
            (0.6595011102219, -2.1359434279405),
            (0.6583348114025, -2.1354884206045),
            (0.6581220034068, -2.1382437718946),
            (0.6594479998527, -2.1384597563896),
            (0.6599990002976, -2.1376771158464),
            (0.659966917655, -2.1364398519396)
        };
        return Utils.DefaultGeometryFactory.CreatePolygon(
            vertices.Select(v => new Coordinate(v.LongitudeDegrees, v.LatitudeDegrees)).Reverse().ToArray());
    }

    [Benchmark]
    public H3Index Indexing_FromLatLng() => H3Index.FromLatLng(SfCoordinate, 9);

    [Benchmark]
    public LatLng Indexing_ToLatLng() => SfIndex.ToLatLng();

    [Benchmark]
    public Polygon Indexing_GetCellBoundary() => SfIndex.GetCellBoundary();

    [Benchmark]
    public bool Inspection_IsValidCell() => SfIndex.IsValidCell;

    [Benchmark]
    public bool Inspection_IsPentagon() => SfIndex.IsPentagon;

    [Benchmark]
    public int Traversal_GridDiskDistancesK3() => SfIndex.GridDiskDistances(3).Count();

    [Benchmark]
    public int Traversal_GridDiskDistancesK10() => SfIndex.GridDiskDistances(10).Count();

    [Benchmark]
    public int Traversal_GridPathCells() => SfIndex.GridPathCells(PathDestination).Count();

    [Benchmark]
    public int Traversal_GridDistance() => SfIndex.GridDistance(PathDestination);

    [Benchmark]
    public bool Traversal_IsNeighbour() => SfIndex.IsNeighbour(SfIndex.GetDirectNeighbour(Direction.I).Item1);

    [Benchmark]
    public H3Index Hierarchy_GetParent() => SfIndex.GetParentForResolution(5);

    [Benchmark]
    public int Hierarchy_GetChildrenPlus2() => SfIndex.GetChildrenForResolution(11).Count();

    [Benchmark]
    public int Sets_CompactCells() => DiskCells.CompactCells().Count;

    [Benchmark]
    public int Sets_UncompactCells() => CompactedCells.UncompactCells(9).Count();

    [Benchmark]
    public H3Index Edges_ToDirectedEdge() => SfIndex.ToDirectedEdge(SfIndex.GetDirectNeighbour(Direction.I).Item1);

    [Benchmark]
    public H3Index Edges_GetDestination() => EdgeIndex.GetDirectedEdgeDestination();

    [Benchmark]
    public double Edges_EdgeLengthMeters() => EdgeIndex.EdgeLengthMeters();

    [Benchmark]
    public double Areas_CellAreaInKmSquared() => SfIndex.CellAreaInKmSquared();

    [Benchmark]
    public int Regions_PolyfillSfAtRes9() => SfPolygon.Fill(9).Count();

    [Benchmark]
    public int Regions_PolyfillSfAtRes10() => SfPolygon.Fill(10).Count();

}
