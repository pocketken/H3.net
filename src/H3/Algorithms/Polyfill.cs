using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.LinearReferencing;

#nullable enable

[assembly: InternalsVisibleTo("H3.Benchmarks")]
namespace H3.Algorithms; 

internal sealed class PositiveLonFilter : ICoordinateSequenceFilter {

    public bool Done => false;

    public bool GeometryChanged => true;

    public void Filter(CoordinateSequence seq, int i) {
        var x = seq.GetX(i);
        seq.SetOrdinate(i, Ordinate.X, x < 0 ? x + 360.0 : x);
    }

}

internal sealed class NegativeLonFilter : ICoordinateSequenceFilter {

    public bool Done => false;

    public bool GeometryChanged => true;

    public void Filter(CoordinateSequence seq, int i) {
        var x = seq.GetX(i);
        seq.SetOrdinate(i, Ordinate.X, x > 0 ? x - 360.0 : x);
    }

}

/// <summary>
/// The vertex testing mode to use when checking containment during
/// polyfill operations.
/// </summary>
public enum VertexTestMode {
    /// <summary>
    /// Specifies that the index's center vertex should be contained
    /// within the geometry.  This matches the polyfill behaviour of
    /// the upstream library.
    /// </summary>
    Center,

    /// <summary>
    /// Specifies that any of the index's boundary vertices can be
    /// contained within the geometry.
    /// </summary>
    Any,

    /// <summary>
    /// Specifies that all of the index's boundary vertices must be
    /// contained within the geometry.
    /// </summary>
    All
}

/// <summary>
/// Polyfill algorithms for H3Index.
/// </summary>
public static class Polyfill {

    private static readonly ICoordinateSequenceFilter NegativeLonFilter = new NegativeLonFilter();

    private static readonly ICoordinateSequenceFilter PositiveLonFilter = new PositiveLonFilter();

    /// <summary>
    /// Returns all of the H3 indexes that are contained within the provided
    /// <see cref="Geometry"/> at the specified resolution.  Supports Polygons
    /// (with holes), MultiPolygons (including disjoint polygons), Points,
    /// LineStrings and (nested) GeometryCollections thereof.
    /// </summary>
    /// <remarks>Geometry coordinates must be provided in WGS84 (EPSG:4326).
    /// For other coordinate systems, either transform the geometry first
    /// (e.g. via ProjNet) or use the <see cref="Fill(Geometry, int, Func{H3Index, bool})"/>
    /// overload and perform containment checks in the desired CRS.</remarks>
    /// <param name="polygon">Containment geometry</param>
    /// <param name="resolution">H3 resolution</param>
    /// <param name="testMode">Specify which <see cref="VertexTestMode"/> to use when checking
    /// index vertex containment.  Defaults to <see cref="VertexTestMode.Center"/></param>.
    /// <returns>Indices that are contained within the geometry</returns>
    public static IEnumerable<H3Index> Fill(this Geometry polygon, int resolution, VertexTestMode testMode = VertexTestMode.Center) {
        if (polygon.IsEmpty) return Enumerable.Empty<H3Index>();

        return polygon switch {
            Point point => new[] { point.Coordinate.ToH3Index(resolution) },
            LineString line => line.Coordinates.TraceCoordinates(resolution),
            GeometryCollection collection and not MultiPolygon => FillCollection(collection, resolution, testMode),
            _ => FillPolygons(polygon, resolution, testMode)
        };
    }

    /// <summary>
    /// Returns all of the H3 indexes that match the provided containment
    /// predicate, flood-filling outward from the (interior) seed cells of the
    /// provided <see cref="Geometry"/>.  The predicate receives each candidate
    /// index and returns whether or not it is part of the fill, allowing custom
    /// containment logic (e.g. containment tests performed in a different CRS,
    /// buffered containment and so on).
    /// </summary>
    /// <remarks>The predicate must produce a connected fill region that
    /// includes the geometry's interior points; cells that are not reachable
    /// from a seed cell via neighbours matching the predicate are not
    /// produced.</remarks>
    /// <param name="geometry">Geometry providing the fill seed(s)</param>
    /// <param name="resolution">H3 resolution</param>
    /// <param name="predicate">Whether or not a candidate index is contained
    /// within the fill area</param>
    /// <returns>Indices that match the containment predicate</returns>
    public static IEnumerable<H3Index> Fill(this Geometry geometry, int resolution, Func<H3Index, bool> predicate) {
        if (geometry.IsEmpty) return Enumerable.Empty<H3Index>();

        var testPoly = geometry.IsTransMeridian() ? SplitGeometry(geometry) : geometry;
        var capacity = EstimateFillCapacity(testPoly, resolution);

        return FillUsingPredicate(testPoly, resolution, capacity, predicate);
    }

    /// <summary>
    /// Parallel equivalent of <see cref="Fill(Geometry, int, VertexTestMode)"/> for large
    /// fills.  The polygon's envelope is split into horizontal strips, each strip is filled
    /// concurrently by the sequential fill over the polygon clipped to that strip, and the
    /// per-strip cell sets are unioned.  The result is identical to <c>Fill</c> (a cell
    /// belongs to whichever strip its center falls in; strip-boundary cells are
    /// de-duplicated), only produced across multiple threads.
    /// </summary>
    /// <remarks>
    /// <para><b>Only use this for large fills.</b>  It exists to spread the cost of a fill
    /// that produces many thousands of cells (roughly res ≥ 10 on a city-sized region, or
    /// any fill taking longer than a few milliseconds) across cores.  For small fills it is
    /// <i>slower</i> than <see cref="Fill(Geometry, int, VertexTestMode)"/>: the fixed cost
    /// of computing the envelope, clipping the polygon to each strip (a full NTS overlay per
    /// strip), spinning up tasks, and unioning the results dominates, and none of it is
    /// amortized when there are only a handful of output cells.  When in doubt, measure —
    /// and default to the sequential <c>Fill</c>.</para>
    /// <para>Unlike sequential <c>Fill</c>, this method materializes its result (it cannot
    /// stream) and allocates per-strip working sets plus the union set, so it does not have
    /// the sequential fill's flat, near-zero allocation profile.  The returned set is
    /// unordered.</para>
    /// <para>Each strip runs the sequential fill, which builds its own point-in-area
    /// locator, so no mutable state is shared across threads; the input geometry is only
    /// read.  <paramref name="maxDegreeOfParallelism"/> defaults to
    /// <see cref="Environment.ProcessorCount"/>; a value ≤ 1 (or a degenerate/empty polygon)
    /// falls back to the sequential fill.</para>
    /// </remarks>
    /// <param name="polygon">Geometry to fill</param>
    /// <param name="resolution">H3 resolution</param>
    /// <param name="testMode">Vertex containment test (see <see cref="VertexTestMode"/>)</param>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent strips; defaults to the
    /// processor count.  A value of 1 or less runs the sequential fill.</param>
    /// <returns>The set of H3 indexes covering the polygon (unordered)</returns>
    public static IEnumerable<H3Index> ParallelFill(this Geometry polygon, int resolution,
            VertexTestMode testMode = VertexTestMode.Center, int? maxDegreeOfParallelism = null) {
        if (polygon.IsEmpty) return Enumerable.Empty<H3Index>();

        var dop = maxDegreeOfParallelism ?? Environment.ProcessorCount;
        var env = polygon.EnvelopeInternal;          // computed once here, before any threads
        var height = env.MaxY - env.MinY;
        if (dop <= 1 || height <= 0) return polygon.Fill(resolution, testMode);

        var factory = polygon.Factory;
        var stripHeight = height / dop;
        var strips = new H3Index[dop][];

        Parallel.For(0, dop, new ParallelOptions { MaxDegreeOfParallelism = dop }, k => {
            var y0 = env.MinY + (k * stripHeight);
            var y1 = k == dop - 1 ? env.MaxY : y0 + stripHeight;

            // Closed strip rectangle spanning the full width of the envelope.  Clipping the
            // polygon to it yields exactly the polygon area whose cell centers fall in this
            // latitude band; boundary cells shared with the neighbouring band are emitted by
            // both and removed by the union below.
            var rect = factory.CreatePolygon(new[] {
                new Coordinate(env.MinX, y0),
                new Coordinate(env.MaxX, y0),
                new Coordinate(env.MaxX, y1),
                new Coordinate(env.MinX, y1),
                new Coordinate(env.MinX, y0),
            });

            Geometry clipped;
            try {
                clipped = polygon.Intersection(rect);
            } catch {
                // Robustness fallback: if the overlay fails on a pathological strip edge,
                // fill the whole polygon for this strip (the union still de-duplicates).
                clipped = polygon;
            }

            // Each strip's fill already yields distinct cells, so just collect them; the
            // only cross-strip duplicates are cells centred on a strip line, removed below.
            strips[k] = clipped.IsEmpty
                ? Array.Empty<H3Index>()
                : clipped.Fill(resolution, testMode).ToArray();
        });

        // De-duplicate across strips with the fill's own pooled open-addressed set rather
        // than a managed HashSet: at large fills the union holds millions of cells, and the
        // rented ulong[] avoids a gen-2 HashSet allocation on every call.
        var total = 0;
        foreach (var strip in strips) total += strip.Length;

        var union = new SearchedSet(total);
        try {
            foreach (var strip in strips) {
                foreach (var cell in strip) union.Add(cell);
            }
            var result = new H3Index[union.Count];
            union.CopyTo(result);
            return result;
        } finally {
            union.Dispose();
        }
    }

    private static IEnumerable<H3Index> FillCollection(GeometryCollection collection, int resolution, VertexTestMode testMode) {
        HashSet<H3Index> indexes = new();

        foreach (var geometry in collection.Geometries) {
            indexes.UnionWith(geometry.Fill(resolution, testMode));
        }

        return indexes;
    }

    private static IEnumerable<H3Index> FillPolygons(Geometry polygon, int resolution, VertexTestMode testMode) {
        var testPoly = polygon.IsTransMeridian() ? SplitGeometry(polygon) : polygon;

        var capacity = EstimateFillCapacity(testPoly, resolution);
        PointInAreaLocator locator = new(testPoly);

        return testMode switch {
            VertexTestMode.All => FillUsingAllVertices(locator, testPoly, resolution, capacity),
            VertexTestMode.Any => FillUsingAnyVertex(locator, testPoly, resolution, capacity),
            VertexTestMode.Center => FillUsingCenterVertex(locator, testPoly, resolution, capacity),
            _ => throw new ArgumentOutOfRangeException(nameof(testMode), "invalid vertex test mode")
        };
    }

    private static IEnumerable<H3Index> GetSeeds(Geometry geometry, int resolution) {
        for (var g = 0; g < geometry.NumGeometries; g += 1) {
            var part = geometry.GetGeometryN(g);

            // Interior seed: the fast path that already covers "fat" regions.
            yield return part.InteriorPoint.Coordinate.ToH3Index(resolution);

            // Boundary trace: seed every cell along each ring edge.  A single interior
            // seed cannot reach cells that are contained but only connected to the seed
            // through NON-contained neighbours (thin slivers / narrow features), so
            // those cells were previously dropped from the fill; tracing the boundary in
            // cells — as upstream libh3's polygonToCells does — makes them reachable.
            // Extra seeds never introduce false positives: every fill mode still gates
            // each candidate on its containment predicate, so only genuinely-contained
            // cells are ever emitted, and cells already found are de-duplicated.
            if (part is Polygon poly) {
                foreach (var cell in TraceBoundary(poly, resolution)) yield return cell;
            }
        }
    }

    /// <summary>
    /// Yields the H3 cells lying along a polygon's exterior and interior (hole) ring
    /// edges, used to seed the flood fill so that no contained cell adjacent to the
    /// boundary is missed on thin/narrow features.
    /// </summary>
    private static IEnumerable<H3Index> TraceBoundary(Polygon poly, int resolution) {
        var rings = new[] { (LineString)poly.Shell }.Concat(poly.Holes);
        foreach (var ring in rings) {
            var coords = ring.Coordinates;
            for (var i = 0; i + 1 < coords.Length; i += 1) {
                var a = coords[i].ToH3Index(resolution);
                var b = coords[i + 1].ToH3Index(resolution);
                if (a == b) { yield return a; continue; }

                // GridPathCellsSize returns -1 (and the span fill returns 0) across
                // pentagon distortion rather than throwing, so no line is materialized:
                // the cells are streamed straight out of a pooled buffer that is
                // returned once the segment is consumed (zero steady-state allocation).
                var size = a.GridPathCellsSize(b);
                if (size <= 0) {
                    // Path length not computable across pentagon distortion; the
                    // segment endpoints still seed the fill's neighbour exploration.
                    yield return a;
                    yield return b;
                    continue;
                }

                var buffer = ArrayPool<H3Index>.Shared.Rent(size);
                try {
                    var count = a.GridPathCells(b, buffer.AsSpan(0, size));
                    if (count == 0) {
                        yield return a;
                        yield return b;
                    } else {
                        for (var j = 0; j < count; j += 1) yield return buffer[j];
                    }
                } finally {
                    ArrayPool<H3Index>.Shared.Return(buffer);
                }
            }
        }
    }

    private static IEnumerable<H3Index> FillUsingPredicate(Geometry testPoly, int resolution, int capacity, Func<H3Index, bool> predicate) {
        // Working sets rented from ArrayPool via SearchedSet / PooledIndexStack;
        // the try/finally returns them to the pool when enumeration completes or
        // is disposed (yield return is legal inside a try/finally).
        SearchedSet searched = new(capacity);
        PooledIndexStack toSearch = new(capacity);
        try {
            foreach (var seed in GetSeeds(testPoly, resolution)) toSearch.Push(seed);

            while (toSearch.Count != 0) {
                var index = toSearch.Pop();
                var neighbours = new HexNeighbourComputer(index);

                for (var direction = Direction.Center; direction < Direction.Invalid; direction += 1) {
                    // GetDirectNeighbour(index, Center) is the identity (it returns
                    // index unchanged), so use index directly and skip that traversal
                    // call; the remaining directions use the rotations-free
                    // specialization (this fill discards the rotation count).
                    var neighbour = direction == Direction.Center
                        ? index
                        : neighbours.Neighbour(direction);
                    if (neighbour == H3Index.Invalid || !searched.Add(neighbour)) continue;
                    if (!predicate(neighbour)) continue;

                    yield return neighbour;
                    toSearch.Push(neighbour);
                }
            }
        } finally {
            toSearch.Dispose();
            searched.Dispose();
        }
    }

    /// <summary>
    /// Performs a polyfill operation utilizing the center <see cref="LatLng"/> of each index produced
    /// during the fill.
    /// </summary>
    private static IEnumerable<H3Index> FillUsingCenterVertex(PointInAreaLocator locator, Geometry testPoly, int resolution, int capacity) {
        // Working sets rented from ArrayPool via SearchedSet / PooledIndexStack;
        // the try/finally returns them to the pool when enumeration completes or
        // is disposed (yield return is legal inside a try/finally).
        SearchedSet searched = new(capacity);
        PooledIndexStack toSearch = new(capacity);
        try {
            foreach (var seed in GetSeeds(testPoly, resolution)) toSearch.Push(seed);

            var coordinate = new Coordinate();

            while (toSearch.Count != 0) {
                var index = toSearch.Pop();
                var neighbours = new HexNeighbourComputer(index);

                for (var direction = Direction.Center; direction < Direction.Invalid; direction += 1) {
                    // GetDirectNeighbour(index, Center) is the identity (it returns
                    // index unchanged), so use index directly and skip that traversal
                    // call; the remaining directions use the rotations-free
                    // specialization (this fill discards the rotation count).
                    var neighbour = direction == Direction.Center
                        ? index
                        : neighbours.Neighbour(direction);
                    if (neighbour == H3Index.Invalid || !searched.Add(neighbour)) continue;

                    var location = locator.Locate(neighbour.ToCoordinate(coordinate));
                    if (location != Location.Interior)
                        continue;

                    yield return neighbour;
                    toSearch.Push(neighbour);
                }
            }
        } finally {
            toSearch.Dispose();
            searched.Dispose();
        }
    }

    /// <summary>
    /// Performs a polyfill operation utilizing any <see cref="LatLng"/> from the cell boundary of each
    /// index produced during the fill.
    /// </summary>
    private static IEnumerable<H3Index> FillUsingAnyVertex(PointInAreaLocator locator, Geometry testPoly, int resolution, int capacity) {
        // Working sets rented from ArrayPool via SearchedSet / PooledIndexStack;
        // the try/finally returns them to the pool when enumeration completes or
        // is disposed (yield return is legal inside a try/finally).
        SearchedSet searched = new(capacity);
        PooledIndexStack toSearch = new(capacity);
        try {
            foreach (var seed in GetSeeds(testPoly, resolution)) toSearch.Push(seed);

            var coordinate = new Coordinate();

            while (toSearch.Count != 0) {
                var index = toSearch.Pop();
                var neighbours = new HexNeighbourComputer(index);

                for (var direction = Direction.Center; direction < Direction.Invalid; direction += 1) {
                    // GetDirectNeighbour(index, Center) is the identity (it returns
                    // index unchanged), so use index directly and skip that traversal
                    // call; the remaining directions use the rotations-free
                    // specialization (this fill discards the rotation count).
                    var neighbour = direction == Direction.Center
                        ? index
                        : neighbours.Neighbour(direction);
                    if (neighbour == H3Index.Invalid || !searched.Add(neighbour)) continue;

                    foreach (var vertex in neighbour.GetCellBoundaryVertices()) {
                        coordinate.X = vertex.LongitudeDegrees;
                        coordinate.Y = vertex.LatitudeDegrees;

                        var location = locator.Locate(coordinate);
                        if (location != Location.Interior)
                            continue;

                        yield return neighbour;
                        toSearch.Push(neighbour);
                        break;
                    }
                }
            }
        } finally {
            toSearch.Dispose();
            searched.Dispose();
        }
    }

    /// <summary>
    /// Performs a polyfill operation utilizing all <see cref="LatLng"/>s from the cell boundary of each
    /// index produced during the fill.
    /// </summary>
    private static IEnumerable<H3Index> FillUsingAllVertices(PointInAreaLocator locator, Geometry testPoly, int resolution, int capacity) {
        // Working sets rented from ArrayPool via SearchedSet / PooledIndexStack;
        // the try/finally returns them to the pool when enumeration completes or
        // is disposed (yield return is legal inside a try/finally).
        SearchedSet searched = new(capacity);
        PooledIndexStack toSearch = new(capacity);
        try {
            foreach (var seed in GetSeeds(testPoly, resolution)) toSearch.Push(seed);

            var coordinate = new Coordinate();

            while (toSearch.Count != 0) {
                var index = toSearch.Pop();
                var neighbours = new HexNeighbourComputer(index);

                for (var direction = Direction.Center; direction < Direction.Invalid; direction += 1) {
                    // GetDirectNeighbour(index, Center) is the identity (it returns
                    // index unchanged), so use index directly and skip that traversal
                    // call; the remaining directions use the rotations-free
                    // specialization (this fill discards the rotation count).
                    var neighbour = direction == Direction.Center
                        ? index
                        : neighbours.Neighbour(direction);
                    if (neighbour == H3Index.Invalid || !searched.Add(neighbour)) continue;

                    var matched = true;

                    foreach (var vertex in neighbour.GetCellBoundaryVertices()) {
                        coordinate.X = vertex.LongitudeDegrees;
                        coordinate.Y = vertex.LatitudeDegrees;

                        var location = locator.Locate(coordinate);
                        if (location == Location.Interior)
                            continue;

                        matched = false;
                        break;
                    }

                    if (!matched) continue;

                    yield return neighbour;
                    toSearch.Push(neighbour);
                }
            }
        } finally {
            toSearch.Dispose();
            searched.Dispose();
        }
    }

    /// <summary>
    /// Returns all of the H3 indexes that follow the provided LineString
    /// at the specified resolution.
    /// </summary>
    /// <param name="polyLine"></param>
    /// <param name="resolution"></param>
    /// <returns></returns>
    public static IEnumerable<H3Index> Fill(this LineString polyLine, int resolution) =>
        polyLine.Coordinates.TraceCoordinates(resolution);

    /// <summary>
    /// Gets all of the H3 indices that define the provided set of <see cref="Coordinate"/>s.
    /// </summary>
    /// <param name="coordinates"></param>
    /// <param name="resolution"></param>
    /// <returns></returns>
    public static IEnumerable<H3Index> TraceCoordinates(this Coordinate[] coordinates, int resolution) {
        HashSet<H3Index> indices = new();

        // trace the coordinates
        var coordLen = coordinates.Length - 1;
        LatLng v1 = new();
        LatLng v2 = new();
        for (var c = 0; c < coordLen; c += 1) {
            // from this coordinate to next/first
            var vA = coordinates[c];
            var vB = coordinates[c + 1];
            v1.Longitude = vA.X * M_PI_180;
            v1.Latitude = vA.Y * M_PI_180;
            v2.Longitude = vB.X * M_PI_180;
            v2.Latitude = vB.Y * M_PI_180;

            // estimate number of indices between points, use that as a
            // number of segments to chop the line into
            var count = v1.LineHexEstimate(v2, resolution);

            for (var j = 1; j < count; j += 1) {
                // interpolate line
                var interpolated = LinearLocation.PointAlongSegmentByFraction(vA, vB, (double)j / count);
                indices.Add(interpolated.ToH3Index(resolution));
            }
        }

        return indices;
    }

    /// <summary>
    /// Determines whether or not the geometry is flagged as transmeridian;
    /// that is, has an arc > 180 deg lon.
    /// </summary>
    /// <param name="geometry"></param>
    /// <returns></returns>
    public static bool IsTransMeridian(this Geometry geometry) {
        if (geometry.IsEmpty) return false;
        var coords = geometry.Envelope.Coordinates;
        return Math.Abs(coords[0].X - coords[2].X) > 180.0;
    }

    /// <summary>
    /// Attempts to split a polygon that spans the antemeridian into
    /// a multipolygon by clipping coordinates on either side of it and
    /// then unioning them back together again.
    /// </summary>
    /// <param name="originalGeometry"></param>
    /// <returns></returns>
    internal static Geometry SplitGeometry(Geometry originalGeometry) {
        var left = originalGeometry.Copy();
        left.Apply(NegativeLonFilter);
        var right = originalGeometry.Copy();
        right.Apply(PositiveLonFilter);

        var geometry = left.Union(right);
        return geometry.IsEmpty ? originalGeometry : geometry;
    }

    private const int MinFillCapacity = 16;

    private const int MaxFillCapacity = 1 << 18;

    /// <summary>
    /// Estimates an initial capacity for the flood-fill working sets (the
    /// searched-cell dedup set and the to-search stack) from the geometry's
    /// bounding-box area and the average cell area at the target resolution.
    /// This is purely a capacity hint — it never changes which cells are
    /// produced — that lets the working sets be allocated once at roughly the
    /// right size instead of repeatedly doubling and rehashing (and churning the
    /// large object heap) as they grow from empty.
    /// </summary>
    private static int EstimateFillCapacity(Geometry geometry, int resolution) {
        if (resolution < 0 || resolution > MAX_H3_RES) return MinFillCapacity;

        var envelope = geometry.EnvelopeInternal;
        if (envelope.IsNull) return MinFillCapacity;

        // bounding-box area in km^2 using a cheap equirectangular approximation
        // (more than accurate enough for a capacity hint): a degree of latitude
        // spans a constant distance, a degree of longitude shrinks with the
        // cosine of latitude.
        var kmPerDegree = EARTH_RADIUS_KM * M_PI_180;
        var midLatitudeRadians = (envelope.MinY + envelope.MaxY) * 0.5 * M_PI_180;
        var heightKm = envelope.Height * kmPerDegree;
        var widthKm = envelope.Width * kmPerDegree * Math.Cos(midLatitudeRadians);
        var areaKm2 = Math.Abs(heightKm * widthKm);

        var estimate = areaKm2 / LookupTables.HexgonAreasInKm2[resolution];
        if (!(estimate > MinFillCapacity)) return MinFillCapacity;   // NaN / tiny
        return estimate >= MaxFillCapacity ? MaxFillCapacity : (int)estimate;
    }

    /// <summary>
    /// An allocation-free point-in-area locator used by the flood fill in place
    /// of NetTopologySuite's <see cref="IndexedPointInAreaLocator"/>.  The stock
    /// locator allocates a fresh ray-crossing counter and segment visitor on
    /// <em>every</em> <see cref="Locate"/> call — the fill's hottest inner op,
    /// invoked once per distinct cell examined — which dominates the polyfill's
    /// measured allocation.  This locator instead flattens the geometry's
    /// boundary segments once (via the same <see cref="LinearComponentExtracter"/>
    /// NTS itself uses) and evaluates the identical ray-crossing rule inline over
    /// that flat array with no per-call allocation, spatial-index tree walk or
    /// interface dispatch.
    ///
    /// The ray-crossing algebra is a faithful transcription of NTS's
    /// <c>RayCrossingCounter</c>: the same half-open (strictly-above /
    /// at-or-below) straddle convention, the same on-vertex and horizontal-edge
    /// handling and the same robust <see cref="Orientation.Index"/> /
    /// <see cref="Orientation.ReOrient"/> predicate.  It therefore produces
    /// bit-identical <see cref="Location"/> results — visiting every segment is
    /// equivalent to NTS visiting only the Y-index-selected subset, because a
    /// segment that does not straddle the ray contributes nothing to the count.
    /// <see cref="Location.Boundary"/> is treated as not-interior by the fill
    /// exactly as before, so the produced cell set is unchanged.
    ///
    /// For geometries with more than <see cref="MaxFlatSegments"/> boundary
    /// segments — where the flat O(V) scan could lose to the spatial index — it
    /// transparently defers to the stock <see cref="IndexedPointInAreaLocator"/>,
    /// preserving that path's behaviour and cost for large polygons.
    /// </summary>
    private sealed class PointInAreaLocator {

        // Above this boundary-segment count the flat linear scan is no longer a
        // clear win over NTS's Y-interval index, so fall back to the stock
        // locator to avoid regressing large-polygon fills.
        private const int MaxFlatSegments = 64;

        private readonly Coordinate[] _segmentStarts;
        private readonly Coordinate[] _segmentEnds;
        private readonly int _segmentCount;
        private readonly IndexedPointInAreaLocator? _fallback;

        public PointInAreaLocator(Geometry geometry) {
            var lines = LinearComponentExtracter.GetLines(geometry);

            var total = 0;
            foreach (Geometry line in lines) {
                var points = line.NumPoints;
                if (points > 1) total += points - 1;
            }

            if (total > MaxFlatSegments) {
                _fallback = new IndexedPointInAreaLocator(geometry);
                _segmentStarts = Array.Empty<Coordinate>();
                _segmentEnds = Array.Empty<Coordinate>();
                return;
            }

            var starts = new Coordinate[total];
            var ends = new Coordinate[total];
            var k = 0;
            foreach (Geometry line in lines) {
                var coordinates = line.Coordinates;
                for (var i = 1; i < coordinates.Length; i += 1) {
                    starts[k] = coordinates[i - 1];
                    ends[k] = coordinates[i];
                    k += 1;
                }
            }

            _segmentStarts = starts;
            _segmentEnds = ends;
            _segmentCount = k;
        }

        /// <summary>
        /// Locates <paramref name="p"/> relative to the geometry, returning
        /// <see cref="Location.Interior"/>, <see cref="Location.Boundary"/> or
        /// <see cref="Location.Exterior"/> — identical to
        /// <see cref="IndexedPointInAreaLocator.Locate"/>.
        /// </summary>
        public Location Locate(Coordinate p) {
            if (_fallback is not null) return _fallback.Locate(p);

            var starts = _segmentStarts;
            var ends = _segmentEnds;
            var count = _segmentCount;
            var px = p.X;
            var py = p.Y;
            var crossings = 0;

            for (var s = 0; s < count; s += 1) {
                var a = starts[s];
                var b = ends[s];
                double ax = a.X, ay = a.Y, bx = b.X, by = b.Y;

                // both endpoints strictly left of the ray origin: the rightward
                // ray cannot cross this segment
                if (ax < px && bx < px) continue;

                // query point coincides with the segment's end vertex
                if (px == bx && py == by) return Location.Boundary;

                // horizontal segment at the ray height: on the boundary iff the
                // point lies within its x-extent, otherwise it does not count
                if (ay == py && by == py) {
                    double minx, maxx;
                    if (ax < bx) { minx = ax; maxx = bx; } else { minx = bx; maxx = ax; }
                    if (px >= minx && px <= maxx) return Location.Boundary;
                    continue;
                }

                // does the segment straddle the horizontal ray?  half-open in y
                // (strictly-above / at-or-below) so shared vertices count once
                if ((ay > py && by <= py) || (by > py && ay <= py)) {
                    var orientation = Orientation.Index(a, b, p);
                    if (orientation == OrientationIndex.Collinear) return Location.Boundary;

                    // normalise so the effective segment direction is upward
                    if (by < ay) orientation = Orientation.ReOrient(orientation);

                    // an upward segment crosses the rightward ray when the point
                    // lies to its left
                    if (orientation == OrientationIndex.Left) crossings += 1;
                }
            }

            // odd crossing parity => interior
            return (crossings & 1) == 1 ? Location.Interior : Location.Exterior;
        }

    }

    /// <summary>
    /// Computes the six direct hexagon neighbours of a single flood-fill cell
    /// while hoisting the per-cell invariants — resolution, leaf digit, base-cell
    /// pentagon flag and the resolution-class traversal table — out of the six
    /// <see cref="H3HierarchyExtensions.GetDirectNeighbourWithoutRotations"/> calls
    /// the fill would otherwise make per cell (each of which independently
    /// re-derives all of them before its one-digit table step).  Neighbours are
    /// computed ~6x as often as centres are projected, so this per-cell setup runs
    /// on the fill's hottest non-projection path.
    ///
    /// For the overwhelmingly common interior move — a res &gt; 0 cell on a
    /// non-pentagon base cell whose leaf-digit adjustment does not carry into the
    /// parent (the traversal table's packed "next ap7 move" is
    /// <see cref="Direction.Center"/>) — the neighbour is the origin with a single
    /// leaf digit rewritten, produced inline with one table load and one
    /// masked-insert.  Every other case (a carry into a coarser digit, a base-cell
    /// crossing, a pentagon base cell, a res-0 origin or an invalid leaf digit)
    /// defers to the full traversal, so the returned index is bit-for-bit
    /// identical to <c>origin.GetDirectNeighbourWithoutRotations(direction)</c> for
    /// all inputs — no floating point is touched and the produced cell set and
    /// traversal order are unchanged.
    /// </summary>
    private readonly struct HexNeighbourComputer {

        private readonly H3Index _origin;
        // null when the origin is not eligible for the inline fast path, in which
        // case every direction defers to the full traversal.
        private readonly byte[]? _table;
        private readonly int _leafTimes7;
        private readonly int _shift;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HexNeighbourComputer(H3Index origin) {
            _origin = origin;
            var resolution = origin.Resolution;
            var leaf = (int)origin.GetDirectionForResolution(resolution);

            if (resolution > 0 && leaf != (int)Direction.Invalid &&
                !BaseCells.IsPentagonCellNumber(origin.BaseCellNumber)) {
                // Same table selection the traversal makes for the leaf resolution:
                // IsResolutionClass3(res) ? Class2 : Class3.
                _table = Utils.IsResolutionClass3(resolution)
                    ? LookupTables.TraversalPackedClass2
                    : LookupTables.TraversalPackedClass3;
                _leafTimes7 = leaf * 7;
                _shift = (MAX_H3_RES - resolution) * H3Index.H3_PER_DIGIT_OFFSET;
            } else {
                _table = null;
                _leafTimes7 = 0;
                _shift = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public H3Index Neighbour(Direction direction) {
            var table = _table;
            if (table is not null) {
                var packed = table[_leafTimes7 + (int)direction];

                // packed >> 3 is the ap7 move to propagate to the coarser
                // resolution; Center (0) means the move stays within this base cell
                // with no carry, so only the leaf digit changes — bit-for-bit the
                // traversal's early break + non-pentagon fast return.
                if ((packed >> 3) == 0) {
                    return new H3Index(
                        (_origin.Value & ~(H3Index.H3_DIGIT_MASK << _shift)) |
                        ((ulong)(packed & 7) << _shift));
                }
            }

            return _origin.GetDirectNeighbourWithoutRotations(direction);
        }

    }

    /// <summary>
    /// A minimal open-addressed, linear-probed set of the <see cref="H3Index"/>
    /// cell values already examined by a flood fill.  Cell values are never zero
    /// (zero is <see cref="H3Index.Invalid"/>, which is filtered out before being
    /// added), so zero marks an empty slot.  This mirrors the probe table used by
    /// the grid-disk traversal and replaces a <see cref="HashSet{T}"/>: the single
    /// contiguous <see cref="ulong"/> backing array both allocates far less than
    /// the hash set's bucket + entry arrays and probes with better cache locality
    /// on the fill's hottest operation — a membership test per neighbour per cell.
    /// The table doubles and rehashes if the presized capacity is exceeded, so
    /// membership semantics are identical to the hash set regardless of estimate.
    /// </summary>
    private sealed class SearchedSet {

        private ulong[]? _slots;
        private int _count;
        private int _growAt;

        public SearchedSet(int capacity) {
            // grow the power-of-two table until `capacity` values fit under a
            // 0.75 load factor (capacity <= 0.75 * size, i.e. size >= capacity * 4 / 3)
            var size = 8;
            var desired = (long)capacity * 4 / 3 + 1;
            while (size < desired && size < (1 << 30)) size <<= 1;

            // Rent the backing table from the shared pool rather than allocating a
            // fresh (often >85 KB, i.e. large-object-heap) array on every fill.  The
            // pool returns a power-of-two-length array (>= size) whose contents are
            // undefined, so clear it for the empty-slot (0) sentinel — the same zero
            // cost `new ulong[]` already paid — and derive mask/growAt from the
            // actual rented Length, never the requested size.
            var slots = ArrayPool<ulong>.Shared.Rent(size);
            Array.Clear(slots, 0, slots.Length);
            _slots = slots;
            _growAt = slots.Length - (slots.Length >> 2);
        }

        /// <summary>Number of distinct values currently held.</summary>
        public int Count => _count;

        /// <summary>
        /// Copies the live (non-zero) values into <paramref name="destination"/>, which
        /// must have room for at least <see cref="Count"/> entries.  Order is unspecified
        /// (open-addressed slot order), which matches the fill's set semantics.
        /// </summary>
        public void CopyTo(Span<H3Index> destination) {
            var slots = _slots!;
            var n = 0;
            foreach (var value in slots) {
                if (value != 0) destination[n++] = value;
            }
        }

        /// <summary>
        /// Adds a (non-zero) value, returning false if it was already present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(ulong value) {
            if (_count >= _growAt) Grow();

            var slots = _slots!;
            var mask = slots.Length - 1;
            var slot = (int)((value * 0x9E3779B97F4A7C15UL) >> 32) & mask;
            while (true) {
                var existing = slots[slot];
                if (existing == 0) {
                    slots[slot] = value;
                    _count += 1;
                    return true;
                }

                if (existing == value) return false;
                slot = (slot + 1) & mask;
            }
        }

        private void Grow() {
            var old = _slots!;
            var newSize = old.Length << 1;
            if (newSize <= old.Length) return;   // table already at maximum size

            var slots = ArrayPool<ulong>.Shared.Rent(newSize);
            Array.Clear(slots, 0, slots.Length);
            var mask = slots.Length - 1;

            // all existing values are unique and non-zero, so reinsert without
            // duplicate checks
            foreach (var value in old) {
                if (value == 0) continue;
                var slot = (int)((value * 0x9E3779B97F4A7C15UL) >> 32) & mask;
                while (slots[slot] != 0) slot = (slot + 1) & mask;
                slots[slot] = value;
            }

            ArrayPool<ulong>.Shared.Return(old);
            _slots = slots;
            _growAt = slots.Length - (slots.Length >> 2);
        }

        /// <summary>
        /// Returns the rented backing table to the shared pool.  Idempotent, so it
        /// is safe to call from an iterator's <c>finally</c> on every completion or
        /// disposal.
        /// </summary>
        public void Dispose() {
            var slots = _slots;
            if (slots is null) return;
            _slots = null;
            ArrayPool<ulong>.Shared.Return(slots);
        }

    }

    /// <summary>
    /// A minimal LIFO stack of the <see cref="H3Index"/> cells still to be visited
    /// by a flood fill, backed by a <see cref="ulong"/> array rented from
    /// <see cref="ArrayPool{T}.Shared"/> (<see cref="H3Index"/> converts implicitly
    /// to/from <see cref="ulong"/>).  It replaces a <see cref="Stack{T}"/> whose
    /// freshly-allocated backing array — ~93 KB, on the large object heap, at the
    /// res-10 benchmark's presized capacity — was allocated, and needlessly
    /// zero-initialized, on every fill.  Renting reuses one array across fills,
    /// removing that per-fill allocation and its GC pressure; the stack tracks its
    /// own count, so no zeroing of the rented array is required.  Push/Pop order is
    /// identical to <see cref="Stack{T}"/>, so the fill visits cells in exactly the
    /// same sequence.
    /// </summary>
    private sealed class PooledIndexStack {

        private ulong[]? _array;
        private int _count;

        public PooledIndexStack(int capacity) {
            _array = ArrayPool<ulong>.Shared.Rent(capacity < MinFillCapacity ? MinFillCapacity : capacity);
        }

        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(H3Index value) {
            var array = _array!;
            if ((uint)_count >= (uint)array.Length) array = Grow();
            array[_count++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public H3Index Pop() => _array![--_count];

        private ulong[] Grow() {
            var old = _array!;
            var newArray = ArrayPool<ulong>.Shared.Rent(old.Length << 1);
            Array.Copy(old, newArray, _count);
            ArrayPool<ulong>.Shared.Return(old);
            _array = newArray;
            return newArray;
        }

        /// <summary>
        /// Returns the rented backing array to the shared pool.  Idempotent.
        /// </summary>
        public void Dispose() {
            var array = _array;
            if (array is null) return;
            _array = null;
            ArrayPool<ulong>.Shared.Return(array);
        }

    }

}