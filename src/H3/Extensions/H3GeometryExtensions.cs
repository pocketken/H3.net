using System;
using System.Collections.Generic;
using System.Linq;
using H3.Model;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Extensions;

public static class H3GeometryExtensions {

    /// <summary>
    /// Determines the spherical coordinates of the center point of a <see cref="H3Index"/>.
    /// </summary>
    /// <param name="inputIndex"></param>
    /// <param name="result">optional result object; defaults to new <see cref="Coordinate"/>
    /// instance.</param>
    /// <returns></returns>
    public static Coordinate ToCoordinate(this H3Index inputIndex, Coordinate? result = default) {
        result ??= new Coordinate();

        // Delegate to the single inverse projection (FaceIJK.ToLatLng -> the vec3
        // pipeline that matches libh3 v4.5.0 bit-for-bit).  Keeping one code path
        // for the cell centre guarantees ToCoordinate and ToLatLng never diverge.
        var center = inputIndex.ToFaceIJK().ToLatLng(inputIndex.Resolution);
        result.X = center.LongitudeDegrees;
        result.Y = center.LatitudeDegrees;

        return result;
    }

    /// <summary>
    /// Convert a <see cref="Coordinate"/> to a H3 index at the specified resolution.
    /// </summary>
    /// <param name="coordinate"></param>
    /// <param name="resolution"></param>
    /// <returns></returns>
    public static H3Index ToH3Index(this Coordinate coordinate, int resolution) {
        if (resolution is < 0 or > MAX_H3_RES) return H3Index.Invalid;
#if NETSTANDARD2_0
            if (!coordinate.X.IsFinite() || !coordinate.Y.IsFinite()) return H3Index.Invalid;
#else
        if (!double.IsFinite(coordinate.X) || !double.IsFinite(coordinate.Y)) return H3Index.Invalid;
#endif
        return H3Index.FromFaceIJK(
            FaceIJK.FromLatLng(
                coordinate.X * M_PI_180,
                coordinate.Y * M_PI_180,
                resolution
            ),
            resolution
        );
    }

    /// <summary>
    /// Find all icosahedron faces intersected by a given H3 index, represented
    /// as integers from 0-19. The results are sparse; since 0 is a valid value,
    /// invalid values are represented as -1. It is the responsibility of the
    /// caller to filter out invalid values.
    /// </summary>
    /// <returns>Faces intersected by the index</returns>
    public static int[] GetFaces(this H3Index index) {
        while (true) {
            var resolution = index.Resolution;
            var isPentagon = index.IsPentagon;

            // We can't use the vertex-based approach here for class II pentagons,
            // because all their vertices are on the icosahedron edges. Their
            // direct child pentagons cross the same faces, so use those instead.
            if (isPentagon && !IsResolutionClass3(resolution)) {
                // Note that this would not work for res 15, but this is only run on
                // Class II pentagons, it should never be invoked for a res 15 index.
                index = index.GetDirectChild(Direction.Center);
                continue;
            }

            // convert to FaceIJK
            var fijk = index.ToFaceIJK();

            // Get all vertices as FaceIJK addresses. For simplicity, always
            // initialize the array with 6 verts, ignoring the last one for pentagons
            int vertexCount;
            FaceIJK[] vertices;

            if (isPentagon) {
                vertexCount = NUM_PENT_VERTS;
                vertices = fijk.GetPentagonVertices(ref resolution);
            } else {
                vertexCount = NUM_HEX_VERTS;
                vertices = fijk.GetHexVertices(ref resolution);
            }

            // We may not use all of the slots in the output array,
            // so fill with invalid values to indicate unused slots
            var result = new int[isPentagon ? 5 : 2];
#if NETSTANDARD2_0
                for (var i = 0; i < result.Length; i += 1) {
                    result[i] = -1;
                }
#else
            Array.Fill(result, -1);
#endif

            // add each vertex face, using the output array as a hash set
            for (var i = 0; i < vertexCount; i += 1) {
                var vert = vertices[i];

                // Adjust overage, determining whether this vertex is
                // on another face
                if (isPentagon) {
                    vert.AdjustPentagonVertexOverage(resolution);
                } else {
                    vert.AdjustOverageClass2(resolution, false, true);
                }

                // Save the face to the output array
                var face = vert.Face;
                var pos = 0;

                // Find the first empty output position, or the first position
                // matching the current face
                while (result[pos] != -1 && result[pos] != face)
                    pos++;

                result[pos] = face;
            }

            return result;
        }
    }

    /// <summary>
    /// Area of H3 cell in radians^2.
    ///
    /// The area is computed from the cell boundary loop using the Cagnoli
    /// spherical area formula with compensated summation.  Note that some H3
    /// cells (hexagons and pentagons) are irregular, and have more than 6 or
    /// 5 sides.
    /// </summary>
    /// <param name="index">H3 cell</param>
    /// <returns>area in radians^2</returns>
    public static double CellAreaInRadiansSquared(this H3Index index) {
        var resolution = index.Resolution;
        var faceIjk = index.ToFaceIJK();

        // fill the boundary straight into a stack buffer (NUM_HEX_VERTS vertices
        // plus up to NUM_HEX_VERTS edge-crossing intersections) so the whole
        // area computation is allocation-free
        Span<LatLng> boundary = stackalloc LatLng[NUM_HEX_VERTS * 2 + 4];
        var count = index.IsPentagon
            ? faceIjk.GetPentagonBoundary(resolution, 0, NUM_PENT_VERTS, boundary)
            : faceIjk.GetHexagonBoundary(resolution, 0, NUM_HEX_VERTS, boundary);

        return LatLng.GetLoopAreaInRadiansSquared(boundary[..count]);
    }

    /// <summary>
    /// Area of H3 cell in kilometers^2.
    /// </summary>
    /// <param name="index">H3 cell</param>
    /// <returns>area in km^2</returns>
    public static double CellAreaInKmSquared(this H3Index index) =>
        CellAreaInRadiansSquared(index) * EARTH_RADIUS_KM * EARTH_RADIUS_KM;

    /// <summary>
    /// Area of H3 cell in m^2.
    /// </summary>
    /// <param name="index">H3 cell</param>
    /// <returns></returns>
    public static double CellAreaInMSquared(this H3Index index) =>
        CellAreaInKmSquared(index) * 1000.0 * 1000.0;

    /// <summary>
    /// Determines the radius of a given cell in Km
    /// </summary>
    /// <param name="index">H3Index to get area for</param>
    /// <returns></returns>
    public static double GetRadiusInKm(this H3Index index) {
        var resolution = index.Resolution;
        var faceIjk = index.ToFaceIJK();
        var center = faceIjk.ToLatLng(resolution);
        var firstVertex =  (index.IsPentagon
            ? faceIjk.GetPentagonBoundary(resolution, 0, 1)
            : faceIjk.GetHexagonBoundary(resolution, 0, 1)).First();
        return center.GetGreatCircleDistanceInKm(firstVertex);
    }

    /// <summary>
    /// Determines the cell boundary vertices in spherical coordinates for
    /// a given H3 index.
    /// </summary>
    /// <param name="index">H3Index to get boundary for</param>
    /// <returns>boundary coordinates</returns>
    public static IEnumerable<LatLng> GetCellBoundaryVertices(this H3Index index) {
        var face = index.ToFaceIJK();
        var resolution = index.Resolution;
        return index.IsPentagon
            ? face.GetPentagonBoundary(resolution, 0, NUM_PENT_VERTS)
            : face.GetHexagonBoundary(resolution, 0, NUM_HEX_VERTS);
    }

    /// <summary>
    /// The maximum number of vertices in a cell boundary, i.e. the minimum length
    /// of the destination buffer for the <see cref="Span{T}"/> overload of
    /// <see cref="GetCellBoundaryVertices(H3Index,Span{LatLng})"/>.  A Class III
    /// hexagon boundary is at most <see cref="Constants.NUM_HEX_VERTS"/> vertices
    /// plus one edge-crossing vertex per side.
    /// </summary>
    public const int MaxCellBoundaryVertices = NUM_HEX_VERTS * 2;

    /// <summary>
    /// Fills <paramref name="destination"/> with the cell boundary vertices in
    /// spherical coordinates for the given index and returns the number written.
    /// Allocation-free equivalent of the streaming
    /// <see cref="GetCellBoundaryVertices(H3Index)"/>, with identical vertices and
    /// ordering.
    /// </summary>
    /// <param name="index">H3Index to get boundary for</param>
    /// <param name="destination">buffer of at least
    /// <see cref="MaxCellBoundaryVertices"/> vertices</param>
    /// <returns>the number of vertices written to <paramref name="destination"/></returns>
    /// <exception cref="ArgumentException">Thrown when the destination buffer is
    /// smaller than <see cref="MaxCellBoundaryVertices"/>.</exception>
    public static int GetCellBoundaryVertices(this H3Index index, Span<LatLng> destination) {
        if (destination.Length < MaxCellBoundaryVertices) {
            throw new ArgumentException(
                $"destination must hold at least {MaxCellBoundaryVertices} vertices (see {nameof(MaxCellBoundaryVertices)})", nameof(destination));
        }

        var face = index.ToFaceIJK();
        var resolution = index.Resolution;
        return index.IsPentagon
            ? face.GetPentagonBoundary(resolution, 0, NUM_PENT_VERTS, destination)
            : face.GetHexagonBoundary(resolution, 0, NUM_HEX_VERTS, destination);
    }

    /// <summary>
    /// Generates a Polygon of the cell boundary for a given H3 index.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="geomFactory">Optional GeometryFactory to be used to create
    /// Polygon instance.  Note that vertex coordinates are provided in EPSG
    /// 4326 (WGS84)</param>
    /// <returns>Polygon for cell boundary</returns>
    public static Polygon GetCellBoundary(this H3Index index, GeometryFactory? geomFactory = null) {
        var face = index.ToFaceIJK();
        var resolution = index.Resolution;

        // fill the boundary straight into a stack buffer (NUM_HEX_VERTS vertices
        // plus up to NUM_HEX_VERTS edge-crossing intersections) so we avoid the
        // intermediate LatLng[] and List<Coordinate> allocations
        Span<LatLng> boundary = stackalloc LatLng[NUM_HEX_VERTS * 2 + 4];
        var count = index.IsPentagon
            ? face.GetPentagonBoundary(resolution, 0, NUM_PENT_VERTS, boundary)
            : face.GetHexagonBoundary(resolution, 0, NUM_HEX_VERTS, boundary);

        // build the closed ring directly at the right size: vertices followed by
        // a copy of the first vertex to close the hole
        var coordinates = new Coordinate[count + 1];
        for (var i = 0; i < count; i += 1) {
            coordinates[i] = new Coordinate(boundary[i].LongitudeDegrees, boundary[i].LatitudeDegrees);
        }
        coordinates[count] = coordinates[0].Copy();

        var gf = geomFactory ?? DefaultGeometryFactory;
        return gf.CreatePolygon(coordinates);
    }

    /// <summary>
    /// Generates a Multi-Polygon containing all of the cell boundaries for
    /// a given set of H3 indices.
    /// </summary>
    /// <param name="indices"></param>
    /// <param name="geomFactory"></param>
    /// <returns></returns>
    public static MultiPolygon GetCellBoundaries(this IEnumerable<H3Index> indices, GeometryFactory? geomFactory = null) {
        var gf = geomFactory ?? DefaultGeometryFactory;
        return gf.CreateMultiPolygon(indices.Select(index => index.GetCellBoundary(gf)).ToArray());
    }

    /// <summary>
    /// Generates a MultiPolygon of the dissolved outline(s) of a set of cells,
    /// i.e. the boundaries of the set with shared edges removed, including any
    /// holes.  Cells must be unique and valid, and all be at the same
    /// resolution.
    /// </summary>
    /// <param name="indexes"></param>
    /// <param name="geomFactory">Optional GeometryFactory to be used to create
    /// the MultiPolygon instance.  Note that vertex coordinates are provided in
    /// EPSG 4326 (WGS84)</param>
    /// <returns>MultiPolygon of the dissolved cell set outlines</returns>
    /// <exception cref="ArgumentException">Thrown when the set contains an
    /// invalid cell index, mixed resolutions or duplicates.</exception>
    public static MultiPolygon CellsToMultiPolygon(this IEnumerable<H3Index> indexes, GeometryFactory? geomFactory = null) {
        var gf = geomFactory ?? DefaultGeometryFactory;

        HashSet<ulong> seen = new();
        List<Geometry> polygons = new();
        var resolution = -1;

        foreach (var index in indexes) {
            if (!index.IsValidCell) {
                throw new ArgumentException($"{index} is not a valid cell index", nameof(indexes));
            }

            if (resolution == -1) {
                resolution = index.Resolution;
            } else if (index.Resolution != resolution) {
                throw new ArgumentException("all indexes must be at the same resolution", nameof(indexes));
            }

            if (!seen.Add(index)) {
                throw new ArgumentException($"{index} is present more than once", nameof(indexes));
            }

            polygons.Add(index.GetCellBoundary(gf));
        }

        if (polygons.Count == 0) {
            return gf.CreateMultiPolygon();
        }

        return NetTopologySuite.Operation.Union.CascadedPolygonUnion.Union(polygons) switch {
            MultiPolygon multiPolygon => multiPolygon,
            Polygon polygon => gf.CreateMultiPolygon(new[] { polygon }),
            _ => gf.CreateMultiPolygon()
        };
    }

}