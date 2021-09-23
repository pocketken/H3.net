using System.Collections.Generic;
using System.Linq;
using H3.Model;

#nullable enable

namespace H3.Extensions {

    /// <summary>
    /// Extends the H3Index class with support for Unidirectional Edge
    /// functionality.
    /// </summary>
    public static class H3UniEdgeExtensions {
        /// <summary>
        /// Returns a unidirectional edge H3 index based on the provided origin and
        /// destination.
        /// </summary>
        /// <param name="origin">Origin H3 index</param>
        /// <param name="destination">Destination H3 index</param>
        /// <returns>The unidirectional edge H3Index, or Invalid on failure.
        /// </returns>
        public static H3Index GetUnidirectionalEdge(this H3Index origin, H3Index destination) {
            Direction direction = origin.DirectionForNeighbour(destination);

            // The direction will be invalid if the cells are not neighbors
            if (direction == Direction.Invalid) {
                return H3Index.Invalid;
            }

            // Create the edge index for the neighbor direction
            return new H3Index(origin) {
                Mode = Mode.UniEdge,
                ReservedBits = (int)direction
            };
        }

        /// <summary>
        /// Provides all of the unidirectional edges from the provided H3 cell
        /// index.
        /// </summary>
        /// <param name="origin">Origin H3 index</param>
        /// <returns>All of the unidirectional edges for the H3 origin index.</returns>
        public static IEnumerable<H3Index> GetUnidirectionalEdges(this H3Index origin) {
            bool isPentagon = origin.IsPentagon;

            // This is actually quite simple. Just modify the bits of the origin
            // slightly for each direction, except the 'k' direction in pentagons,
            // which is zeroed.
            for (int d = 0; d < 6; d += 1) {
                if (isPentagon && d == 0) {
                    yield return H3Index.Invalid;
                    continue;
                }

                yield return new H3Index(origin) {
                    Mode = Mode.UniEdge,
                    ReservedBits = d + 1
                };
            }
        }

        /// <summary>
        /// Returns the origin cell from the unidirectional edge H3Index.
        /// </summary>
        /// <param name="edge">Unidirectional edge H3 index</param>
        /// <returns>The origin cell index, or Invalid on failure</returns>
        public static H3Index GetOriginFromUnidirectionalEdge(this H3Index edge) {
            if (edge.Mode != Mode.UniEdge) {
                return H3Index.Invalid;
            }

            return new H3Index(edge) {
                Mode = Mode.Cell,
                ReservedBits = 0
            };
        }

        /// <summary>
        /// Returns the destination cell from the unidirectional edge H3Index.
        /// </summary>
        /// <param name="edge">Unidirectional edge H3 index</param>
        /// <returns>The destination cell index, or Invalid on failure</returns>
        public static H3Index GetDestinationFromUnidirectionalEdge(this H3Index edge) {
            var origin = GetOriginFromUnidirectionalEdge(edge);
            if (origin == H3Index.Invalid) {
                return H3Index.Invalid;
            }
            return origin.GetDirectNeighbour((Direction)edge.ReservedBits).Item1;
        }

        /// <summary>
        /// Returns the origin, destination pair of cell indexes for the given edge.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static (H3Index, H3Index) GetIndexesFromUnidirectionalEdge(this H3Index edge) =>
            (edge.GetOriginFromUnidirectionalEdge(), edge.GetDestinationFromUnidirectionalEdge());

        /// <summary>
        /// Provides the coordinates defining the unidirectional edge.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static IEnumerable<GeoCoord> GetUnidirectionalEdgeBoundaryVertices(this H3Index edge) {
            if (!edge.IsUnidirectionalEdgeValid()) {
                return Enumerable.Empty<GeoCoord>();
            }
            Direction direction = (Direction)edge.ReservedBits;
            H3Index origin = edge.GetOriginFromUnidirectionalEdge();

            // get the start vertex for the edge
            int startVertex = origin.GetVertexNumberForDirection(direction);
            if (startVertex == H3VertexExtensions.InvalidVertex) {
                return Enumerable.Empty<GeoCoord>();
            }

            FaceIJK face = origin.ToFaceIJK();
            int resolution = origin.Resolution;

            return origin.IsPentagon
                ? face.GetPentagonBoundary(resolution, startVertex, 2)
                : face.GetHexagonBoundary(resolution, startVertex, 2);

        }

        /// <summary>
        /// Length of a unidirectional edge in radians.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static double GetExactEdgeLengthInRadians(this H3Index edge) {
            var vertices = edge.GetUnidirectionalEdgeBoundaryVertices().ToArray();

            double length = 0.0;
            if (vertices.Length == 0) return length;

            for (int i = 0; i < vertices.Length - 1; i += 1) {
                length += vertices[i].GetPointDistanceInRadians(vertices[i + 1]);
            }

            return length;
        }

        /// <summary>
        /// Determines if the provided H3Index is a valid unidirectional edge index.
        /// </summary>
        /// <param name="edge">H3 unidirectional edge index</param>
        /// <returns>true if a valid unidirectional edge index, false otherwise</returns>
        public static bool IsUnidirectionalEdgeValid(this H3Index edge) {
            if (edge.Mode != Mode.UniEdge) {
                return false;
            }

            Direction neighbourDirection = (Direction)edge.ReservedBits;
            if (neighbourDirection is <= Direction.Center or >= Direction.Invalid) {
                return false;
            }

            H3Index origin = edge.GetOriginFromUnidirectionalEdge();
            if (origin.IsPentagon && neighbourDirection == Direction.K) {
                return false;
            }

            return origin.IsValid;
        }
    }
}
