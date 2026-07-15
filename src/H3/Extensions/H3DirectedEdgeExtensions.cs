using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Provides all of the directed edges into the provided H3 cell
    /// index, i.e. all of the edges whose destination is the provided
    /// index.
    /// </summary>
    /// <param name="destination">Destination H3 index</param>
    /// <returns>All of the directed edges into the H3 destination index.</returns>
    public static IEnumerable<H3Index> DestinationToDirectedEdges(this H3Index destination) {
        foreach (var edge in destination.OriginToDirectedEdges()) {
            yield return edge == H3Index.Invalid ? H3Index.Invalid : edge.ReverseDirectedEdge();
        }
    }

    /// <summary>
    /// Returns the directed edge that points in the opposite direction, i.e.
    /// the edge from the destination cell to the origin cell.
    /// </summary>
    /// <param name="edge">Directed edge H3 index</param>
    /// <returns>The reversed directed edge index, or Invalid on failure</returns>
    public static H3Index ReverseDirectedEdge(this H3Index edge) {
        var (origin, destination) = edge.DirectedEdgeToCells();
        return origin == H3Index.Invalid || destination == H3Index.Invalid
            ? H3Index.Invalid
            : destination.ToDirectedEdge(origin);
    }

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
        return origin == H3Index.Invalid ? H3Index.Invalid : origin.GetDirectNeighbourWithoutRotations((Direction)edge.ReservedBits);
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
    /// <exception cref="ArgumentException">Thrown when the provided index is
    /// not a valid directed edge index.</exception>
    public static IEnumerable<LatLng> GetDirectedEdgeBoundaryVertices(this H3Index edge) {
        if (!edge.IsValidDirectedEdge()) {
            throw new ArgumentException("not a valid directed edge index", nameof(edge));
        }
        var direction = (Direction)edge.ReservedBits;
        var origin = edge.GetDirectedEdgeOrigin();

        // get the start vertex for the edge
        var startVertex = origin.GetVertexNumberForDirection(direction);
        if (startVertex == H3VertexExtensions.InvalidVertex) {
            throw new InvalidOperationException($"unable to determine start vertex for edge {edge}");
        }

        var face = origin.ToFaceIJK();
        var resolution = origin.Resolution;

        return origin.IsPentagon
            ? face.GetPentagonBoundary(resolution, startVertex, 2)
            : face.GetHexagonBoundary(resolution, startVertex, 2);
    }

    /// <summary>
    /// Span-filling variant of <see cref="GetDirectedEdgeBoundaryVertices(H3Index)"/>
    /// that writes the edge boundary vertices into a caller-provided buffer and
    /// returns the number written, avoiding the intermediate <c>LatLng[]</c> and
    /// the boxed array enumerator produced by the <see cref="IEnumerable{T}"/>
    /// overload.  Produces exactly the same vertex sequence.  The buffer must
    /// have room for a length-2 boundary plus, for Class III cells, an
    /// edge-crossing intersection vertex.
    /// </summary>
    private static int GetDirectedEdgeBoundaryVertices(this H3Index edge, Span<LatLng> destination) {
        if (!edge.IsValidDirectedEdge()) {
            throw new ArgumentException("not a valid directed edge index", nameof(edge));
        }
        var direction = (Direction)edge.ReservedBits;
        var origin = edge.GetDirectedEdgeOrigin();

        // get the start vertex for the edge
        var startVertex = origin.GetVertexNumberForDirection(direction);
        if (startVertex == H3VertexExtensions.InvalidVertex) {
            throw new InvalidOperationException($"unable to determine start vertex for edge {edge}");
        }

        var face = origin.ToFaceIJK();
        var resolution = origin.Resolution;

        return origin.IsPentagon
            ? face.GetPentagonBoundary(resolution, startVertex, 2, destination)
            : face.GetHexagonBoundary(resolution, startVertex, 2, destination);
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
    /// <remarks>To obtain the average edge length of hexagon cells at a given
    /// resolution instead, see <see cref="H3Index.GetHexagonEdgeLengthAverageInKm"/>.
    /// </remarks>
    /// <param name="edge">Directed edge H3 index</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when the provided index is
    /// not a valid directed edge index, e.g. it is a cell index.</exception>
    public static double EdgeLengthRadians(this H3Index edge) {
        // the boundary is consumed in a single forward pass, so fill a stack
        // buffer and sum consecutive great-circle distances rather than
        // materializing an intermediate array and boxing an array enumerator.
        // A length-2 edge boundary yields at most 2 vertices plus, for Class III
        // cells, one edge-crossing intersection vertex; 16 (the boundary core's
        // scratch-buffer size) cannot overflow.
        Span<LatLng> vertices = stackalloc LatLng[16];
        var count = edge.GetDirectedEdgeBoundaryVertices(vertices);

        var length = 0.0;
        for (var i = 1; i < count; i += 1) {
            length += vertices[i - 1].GetGreatCircleDistanceInRadians(vertices[i]);
        }

        return length;
    }

    /// <summary>
    /// Length of a directed edge in kilometers.
    /// </summary>
    /// <remarks>To obtain the average edge length of hexagon cells at a given
    /// resolution instead, see <see cref="H3Index.GetHexagonEdgeLengthAverageInKm"/>.
    /// </remarks>
    /// <param name="edge">Directed edge H3 index</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when the provided index is
    /// not a valid directed edge index, e.g. it is a cell index.</exception>
    public static double EdgeLengthKilometers(this H3Index edge) {
        return edge.EdgeLengthRadians() * Constants.EARTH_RADIUS_KM;
    }

    /// <summary>
    /// Length of a directed edge in meters.
    /// </summary>
    /// <remarks>To obtain the average edge length of hexagon cells at a given
    /// resolution instead, see <see cref="H3Index.GetHexagonEdgeLengthAverageInM"/>.
    /// </remarks>
    /// <param name="edge">Directed edge H3 index</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when the provided index is
    /// not a valid directed edge index, e.g. it is a cell index.</exception>
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