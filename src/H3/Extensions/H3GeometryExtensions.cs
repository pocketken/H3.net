using System;
using System.Collections.Generic;
using System.Linq;
using H3.Model;
using NetTopologySuite.Geometries;
using static H3.Constants;
using static H3.Utils;

#nullable enable

namespace H3.Extensions {

    public static class H3GeometryExtensions {
        /// <summary>
        /// Find all icosahedron faces intersected by a given H3 index, represented
        /// as integers from 0-19. The results are sparse; since 0 is a valid value,
        /// invalid values are represented as -1. It is the responsibility of the
        /// caller to filter out invalid values.
        /// </summary>
        /// <returns>Faces intersected by the index</returns>
        public static int[] GetFaces(this H3Index index) {
            int resolution = index.Resolution;

            // We can't use the vertex-based approach here for class II pentagons,
            // because all their vertices are on the icosahedron edges. Their
            // direct child pentagons cross the same faces, so use those instead.
            if (index.IsPentagon && !IsResolutionClass3(resolution)) {
                // Note that this would not work for res 15, but this is only run on
                // Class II pentagons, it should never be invoked for a res 15 index.
                return index.GetDirectChild(Direction.Center).GetFaces();
            }

            // convert to FaceIJK
            FaceIJK fijk = index.ToFaceIJK();

            // Get all vertices as FaceIJK addresses. For simplicity, always
            // initialize the array with 6 verts, ignoring the last one for pentagons
            int vertexCount;
            FaceIJK[] vertices;

            if (index.IsPentagon) {
                vertexCount = NUM_PENT_VERTS;
                vertices = fijk.GetPentagonVertices(ref resolution);
            } else {
                vertexCount = NUM_HEX_VERTS;
                vertices = fijk.GetHexVertices(ref resolution);
            }

            // We may not use all of the slots in the output array,
            // so fill with invalid values to indicate unused slots
            int[] result = new int[index.MaximumFaceCount];
            Array.Fill(result, -1);

            // add each vertex face, using the output array as a hash set
            for (int i = 0; i < vertexCount; i += 1) {
                FaceIJK vert = vertices[i];

                // Adjust overage, determining whether this vertex is
                // on another face
                if (index.IsPentagon) {
                    vert.AdjustPentagonVertexOverage(resolution);
                } else {
                    vert.AdjustOverageClass2(resolution, false, true);
                }

                // Save the face to the output array
                int face = vert.Face;
                int pos = 0;

                // Find the first empty output position, or the first position
                // matching the current face
                while (result[pos] != -1 && result[pos] != face) pos++;
                result[pos] = face;
            }

            return result;
        }

        /// <summary>
        /// Area of H3 cell in radians^2.
        ///
        /// The area is calculated by breaking the cell into spherical triangles and
        /// summing up their areas. Note that some H3 cells (hexagons and pentagons)
        /// are irregular, and have more than 6 or 5 sides.
        /// </summary>
        /// <param name="index">H3 cell</param>
        /// <returns>area in radians^2</returns>
        public static double CellAreaInRadiansSquared(this H3Index index) {
            GeoCoord c = index.ToGeoCoord();
            var boundary = index.GetCellBoundaryVertices().ToArray();
            double area = 0.0;

            for (int i = 0; i < boundary.Length; i += 1) {
                int j = (i + 1) % boundary.Length;
                area += GeoCoord.GetTriangleArea(boundary[i], boundary[j], c);
            }

            return area;
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
        /// Determines the radius of a given hexagon in Km
        /// </summary>
        /// <param name="index">H3Index to get area for</param>
        /// <returns></returns>
        public static double GetRadiusInKm(this H3Index index) {
            GeoCoord center = index.ToGeoCoord();
            GeoCoord firstVertex = index.GetCellBoundaryVertices().First();
            return center.GetPointDistanceInKm(firstVertex);
        }

        /// <summary>
        /// Determines the cell boundary vertices in spherical coordinates for
        /// a given H3 index.
        /// </summary>
        /// <param name="index">H3Index to get boundary for</param>
        /// <returns>boundary coordinates</returns>
        public static IEnumerable<GeoCoord> GetCellBoundaryVertices(this H3Index index) {
            FaceIJK face = index.ToFaceIJK();
            int resolution = index.Resolution;
            return index.IsPentagon
                ? face.GetPentagonBoundary(resolution, 0, NUM_PENT_VERTS)
                : face.GetHexagonBoundary(resolution, 0, NUM_HEX_VERTS);
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
            // get vertices and copy first onto the end to close the hole
            var polyVertices = GetCellBoundaryVertices(index).ToList();
            polyVertices.Add(polyVertices.First());
            var gf = geomFactory ?? DefaultGeometryFactory;
            return gf.CreatePolygon(
                polyVertices.Select(vert => new Coordinate(vert.LongitudeDegrees, vert.LatitudeDegrees))
                    .ToArray());
        }

        /// <summary>
        /// Generates a Multi-Polygon containing all of the cell boundaries for
        /// a given set of H3 indicies.
        /// </summary>
        /// <param name="indicies"></param>
        /// <param name="geomFactory"></param>
        /// <returns></returns>
        public static MultiPolygon GetCellBoundaries(this IEnumerable<H3Index> indicies, GeometryFactory? geomFactory = null) {
            var gf = geomFactory ?? DefaultGeometryFactory;
            return gf.CreateMultiPolygon(indicies.Select(index => index.GetCellBoundary()).ToArray());
        }
    }

}
