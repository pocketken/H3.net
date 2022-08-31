using System;
using System.Collections.Generic;
using System.Linq;
using H3.Model;

#nullable enable

namespace H3.Extensions;

/// <summary>
/// Extends the <see cref="H3Index"/> class with support for Directed Edge
/// functionality.
/// </summary>
public static class H3DirectedEdgeExtensions {
    /// <summary>
    /// Returns a directed edge H3 index based on the provided origin and
    /// destination.
    /// </summary>
    /// <param name="origin">Origin H3 index</param>
    /// <param name="destination">Destination H3 index</param>
    /// <returns>The Directed edge H3Index, or Invalid on failure.
    /// </returns>
    [Obsolete("as of 4.0: use ToDirectedEdge instead")]
    public static H3Index GetUnidirectionalEdge(this H3Index origin, H3Index destination) {
        return origin.ToDirectedEdge(destination);
    }

    /// <summary>
    /// Returns a directed edge H3 index based on the provided origin and
    /// destination.
    /// </summary>
    /// <param name="origin">Origin H3 index</param>
    /// <param name="destination">Destination H3 index</param>
    /// <returns>The Directed edge H3Index, or Invalid on failure.
    /// </returns>
    public static H3Index ToDirectedEdge(this H3Index origin, H3Index destination) {
        var direction = origin.DirectionForNeighbour(destination);

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
    /// Provides all of the directed edges from the provided H3 cell
    /// index.
    /// </summary>
    /// <param name="origin">Origin H3 index</param>
    /// <returns>All of the Directed edges for the H3 origin index.</returns>
    [Obsolete("as of 4.0: use OriginToDirectedEdges instead")]
    public static IEnumerable<H3Index> GetUnidirectionalEdges(this H3Index origin) {
        return origin.OriginToDirectedEdges();
    }

    /// <summary>
    /// Provides all of the directed edges from the provided H3 cell
    /// index.
    /// </summary>
    /// <param name="origin">Origin H3 index</param>
    /// <returns>All of the Directed edges for the H3 origin index.</returns>
    public static IEnumerable<H3Index> OriginToDirectedEdges(this H3Index origin) {
        var isPentagon = origin.IsPentagon;

        // This is actually quite simple. Just modify the bits of the origin
        // slightly for each direction, except the 'k' direction in pentagons,
        // which is zeroed.
        for (var d = 0; d < 6; d += 1) {
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

    // TODO implement DestinationToDirectedEdge

    /// <summary>
    /// Returns the origin cell from the given directed edge.
    /// </summary>
    /// <param name="edge">Unidirectional edge H3 index</param>
    /// <returns>The origin cell index, or Invalid on failure</returns>
    [Obsolete("as of 4.0: use GetDirectedEdgeOrigin instead")]
    public static H3Index GetOriginFromUnidirectionalEdge(this H3Index edge) {
        return edge.GetDirectedEdgeOrigin();
    }

    /// <summary>
    /// Returns the origin cell from the given directed edge.
    /// </summary>
    /// <param name="edge">Unidirectional edge H3 index</param>
    /// <returns>The origin cell index, or Invalid on failure</returns>
    public static H3Index GetDirectedEdgeOrigin(this H3Index edge) {
        if (edge.Mode != Mode.UniEdge) {
            return H3Index.Invalid;
        }

        return new H3Index(edge) {
            Mode = Mode.Cell,
            ReservedBits = 0
        };
    }

    /// <summary>
    /// Returns the destination cell from the given directed edge.
    /// </summary>
    /// <param name="edge">Unidirectional edge H3 index</param>
    /// <returns>The destination cell index, or Invalid on failure</returns>
    [Obsolete("as of 4.0: use GetDirectedEdgeDestination instead")]
    public static H3Index GetDestinationFromUnidirectionalEdge(this H3Index edge) {
        return edge.GetDirectedEdgeDestination();
    }

    /// <summary>
    /// Returns the destination cell from the given directed edge.
    /// </summary>
    /// <param name="edge">Unidirectional edge H3 index</param>
    /// <returns>The destination cell index, or Invalid on failure</returns>
    public static H3Index GetDirectedEdgeDestination(this H3Index edge) {
        var origin = GetDirectedEdgeOrigin(edge);
        return origin == H3Index.Invalid ? H3Index.Invalid : origin.GetDirectNeighbour((Direction)edge.ReservedBits).Item1;
    }

    /// <summary>
    /// Returns the origin, destination pair of cell indexes for the given directed edge.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    [Obsolete("as of 4.0: use DirectedEdgeToCells instead")]
    public static (H3Index, H3Index) GetIndexesFromUnidirectionalEdge(this H3Index edge) {
        return edge.DirectedEdgeToCells();
    }

    /// <summary>
    /// Returns the origin, destination pair of cell indexes for the given directed edge.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public static (H3Index, H3Index) DirectedEdgeToCells(this H3Index edge) =>
        (edge.GetDirectedEdgeOrigin(), edge.GetDirectedEdgeDestination());

    /// <summary>
    /// Provides the coordinates defining the directed edge.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    [Obsolete("as of 4.0: use GetDirectedEdgeBoundaryVertices instead")]
    public static IEnumerable<GeoCoord> GetUnidirectionalEdgeBoundaryVertices(this H3Index edge) {
        return (IEnumerable<GeoCoord>)edge.GetDirectedEdgeBoundaryVertices();
    }

    /// <summary>
    /// Provides the coordinates defining the directed edge.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public static IEnumerable<LatLng> GetDirectedEdgeBoundaryVertices(this H3Index edge) {
        if (!edge.IsValidDirectedEdge()) {
            return Enumerable.Empty<LatLng>();
        }
        var direction = (Direction)edge.ReservedBits;
        var origin = edge.GetDirectedEdgeOrigin();

        // get the start vertex for the edge
        var startVertex = origin.GetVertexNumberForDirection(direction);
        if (startVertex == H3VertexExtensions.InvalidVertex) {
            // TODO throw DirectedEdgeInvalid exception?
            return Enumerable.Empty<LatLng>();
        }

        var face = origin.ToFaceIJK();
        var resolution = origin.Resolution;

        return origin.IsPentagon
            ? face.GetPentagonBoundary(resolution, startVertex, 2)
            : face.GetHexagonBoundary(resolution, startVertex, 2);
    }

    /// <summary>
    /// Length of a directed edge in radians.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    [Obsolete("as of 4.0: use EdgeLengthRadians instead")]
    public static double GetExactEdgeLengthInRadians(this H3Index edge) {
        return edge.EdgeLengthRadians();
    }

    /// <summary>
    /// Length of a directed edge in radians.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public static double EdgeLengthRadians(this H3Index edge) {
        var vertices = edge.GetDirectedEdgeBoundaryVertices().ToArray();

        var length = 0.0;
        if (vertices.Length == 0) return length;

        for (var i = 0; i < vertices.Length - 1; i += 1) {
            length += vertices[i].GetGreatCircleDistanceInRadians(vertices[i + 1]);
        }

        return length;
    }

    /// <summary>
    /// Length of a directed edge in kilometers.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public static double EdgeLengthKilometers(this H3Index edge) {
        return edge.EdgeLengthRadians() * Constants.EARTH_RADIUS_KM;
    }

    /// <summary>
    /// Length of a directed edge in kilometers.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public static double EdgeLengthMeters(this H3Index edge) {
        return edge.EdgeLengthKilometers() * 1000;
    }

    /// <summary>
    /// Determines if the provided H3Index is a valid directed edge index.
    /// </summary>
    /// <param name="edge">H3 Directed edge index</param>
    /// <returns>true if a valid Directed edge index, false otherwise</returns>
    [Obsolete("as of 4.0: use IsValidDirectedEdge")]
    public static bool IsUnidirectionalEdgeValid(this H3Index edge) {
        return edge.IsValidDirectedEdge();
    }

    /// <summary>
    /// Determines if the provided H3Index is a valid directed edge index.
    /// </summary>
    /// <param name="edge">H3 Directed edge index</param>
    /// <returns>true if a valid Directed edge index, false otherwise</returns>
    public static bool IsValidDirectedEdge(this H3Index edge) {
        if (edge.Mode != Mode.UniEdge) {
            return false;
        }

        var neighbourDirection = (Direction)edge.ReservedBits;
        if (neighbourDirection is <= Direction.Center or >= Direction.Invalid) {
            return false;
        }

        var origin = edge.GetDirectedEdgeOrigin();
        if (origin.IsPentagon && neighbourDirection == Direction.K) {
            return false;
        }

        return origin.IsValidCell;
    }
}