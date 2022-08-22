using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Geometries;
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
    /// <see cref="Geometry"/> at the specified resolution.  Supports Polygons with holes.
    /// </summary>
    /// <param name="polygon">Containment polygon</param>
    /// <param name="resolution">H3 resolution</param>
    /// <param name="testMode">Specify which <see cref="VertexTestMode"/> to use when checking
    /// index vertex containment.  Defaults to <see cref="VertexTestMode.Center"/></param>.
    /// <returns>Indices that are contained within polygon</returns>
    public static IEnumerable<H3Index> Fill(this Geometry polygon, int resolution, VertexTestMode testMode = VertexTestMode.Center) {
        if (polygon.IsEmpty) return Enumerable.Empty<H3Index>();
        var isTransMeridian = polygon.IsTransMeridian();
        var testPoly = isTransMeridian ? SplitGeometry(polygon) : polygon;

        HashSet<ulong> searched = new();
        Stack<H3Index> toSearch = new();
        toSearch.Push(testPoly.InteriorPoint.Coordinate.ToH3Index(resolution));
        IndexedPointInAreaLocator locator = new(testPoly);

        return testMode switch {
            VertexTestMode.All => FillUsingAllVertices(locator, toSearch, searched),
            VertexTestMode.Any => FillUsingAnyVertex(locator, toSearch, searched),
            VertexTestMode.Center => FillUsingCenterVertex(locator, toSearch, searched),
            _ => throw new ArgumentOutOfRangeException(nameof(testMode), "invalid vertex test mode")
        };
    }

    /// <summary>
    /// Performs a polyfill operation utilizing the center <see cref="LatLng"/> of each index produced
    /// during the fill.
    /// </summary>
    /// <param name="locator"></param>
    /// <param name="toSearch"></param>
    /// <param name="searched"></param>
    /// <returns></returns>
    private static IEnumerable<H3Index> FillUsingCenterVertex(IPointOnGeometryLocator locator, Stack<H3Index> toSearch, ISet<ulong> searched) {
        var coordinate = new Coordinate();
        var faceIjk = new FaceIJK();

        while (toSearch.Count != 0) {
            var index = toSearch.Pop();

            foreach (var neighbour in index.GetNeighbours()) {
                if (searched.Contains(neighbour)) continue;
                searched.Add(neighbour);

                var location = locator.Locate(neighbour.ToCoordinate(coordinate, faceIjk));
                if (location != Location.Interior)
                    continue;

                yield return neighbour;
                toSearch.Push(neighbour);
            }
        }
    }

    /// <summary>
    /// Performs a polyfill operation utilizing any <see cref="LatLng"/> from the cell boundary of each
    /// index produced during the fill.
    /// </summary>
    private static IEnumerable<H3Index> FillUsingAnyVertex(IPointOnGeometryLocator locator, Stack<H3Index> toSearch, ISet<ulong> searched) {
        var coordinate = new Coordinate();
        var faceIjk = new FaceIJK();

        while (toSearch.Count != 0) {
            var index = toSearch.Pop();

            foreach (var neighbour in index.GetNeighbours()) {
                if (searched.Contains(neighbour)) continue;
                searched.Add(neighbour);

                foreach (var vertex in neighbour.GetCellBoundaryVertices(faceIjk)) {
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
    }

    /// <summary>
    /// Performs a polyfill operation utilizing all <see cref="LatLng"/>s from the cell boundary of each
    /// index produced during the fill.
    /// </summary>
    private static IEnumerable<H3Index> FillUsingAllVertices(IPointOnGeometryLocator locator, Stack<H3Index> toSearch, ISet<ulong> searched) {
        var coordinate = new Coordinate();
        var faceIjk = new FaceIJK();

        while (toSearch.Count != 0) {
            var index = toSearch.Pop();

            foreach (var neighbour in index.GetNeighbours()) {
                if (searched.Contains(neighbour)) continue;
                searched.Add(neighbour);

                var matched = true;

                foreach (var vertex in neighbour.GetCellBoundaryVertices(faceIjk)) {
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
        FaceIJK faceIjk = new();
        LatLng v1 = new();
        LatLng v2 = new();
        Vec3d v3d = new();
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
                indices.Add(interpolated.ToH3Index(resolution, faceIjk, v3d));
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

}