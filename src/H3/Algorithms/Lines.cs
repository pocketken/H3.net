using System;
using System.Collections.Generic;
using System.Linq;
using H3.Extensions;
using H3.Model;

namespace H3.Algorithms;

public static class Lines {

    /// <summary>
    /// Produces the grid distance between the two indexes.
    ///
    /// This function may fail to find the distance between two indexes, for
    /// example if they are very far apart. It may also fail when finding
    /// distances for indexes on opposite sides of a pentagon.
    /// </summary>
    /// <param name="origin">index to find distance from</param>
    /// <param name="destination">index to find distance to</param>
    /// <returns>grid distance in cells; -1 if could not be computed</returns>
    [Obsolete("as of 4.0: use GridDistance instead")]
    public static int DistanceTo(this H3Index origin, H3Index destination) {
        return origin.GridDistance(destination);
    }

    /// <summary>
    /// Produces the grid distance between the two indexes.
    ///
    /// This function may fail to find the distance between two indexes, for
    /// example if they are very far apart. It may also fail when finding
    /// distances for indexes on opposite sides of a pentagon.
    /// </summary>
    /// <param name="origin">index to find distance from</param>
    /// <param name="destination">index to find distance to</param>
    /// <returns>grid distance in cells; -1 if could not be computed</returns>
    public static int GridDistance(this H3Index origin, H3Index destination) {
        try {
            var originIjk = LocalCoordIJK.ToLocalIJK(origin, origin);
            var destinationIjk = LocalCoordIJK.ToLocalIJK(origin, destination);

            return originIjk.GetDistanceTo(destinationIjk);
        } catch {
            return -1;
        }
    }

    /// <summary>
    /// Given two H3 cells, return the path of cells between them (inclusive).
    /// </summary>
    /// <remarks>
    /// This function may fail to find the line between two cells, for
    /// example if they are very far apart. It may also fail when finding
    /// distances for indexes on opposite sides of a pentagon.
    /// - The specific output of this function should not be considered stable
    ///   across library versions. The only guarantees the library provides are
    ///   that the line length will be `GridDistance(start, end) + 1` and that
    ///   every index in the line will be a neighbor of the preceding index.
    /// - Lines are drawn in grid space, and may not correspond exactly to either
    ///   Cartesian lines or great arcs.
    /// </remarks>
    /// <param name="origin">start index of the line</param>
    /// <param name="destination">end index of the line</param>
    /// <returns>all points from start to end, inclusive; empty if could not
    /// compute a line</returns>
    [Obsolete("as of 4.0: use GridPathCells instead")]
    public static IEnumerable<H3Index> LineTo(this H3Index origin, H3Index destination) {
        return origin.GridPathCells(destination);
    }

    /// <summary>
    /// Given two H3 cells, return the path of cells between them (inclusive).
    /// </summary>
    /// <remarks>
    /// This function may fail to find the line between two cells, for
    /// example if they are very far apart.  When the path from origin to
    /// destination crosses pentagon distortion relative to the origin's local
    /// coordinate chart, the interpolation is retried anchored at the
    /// destination instead, resolving cases where the chart is discontinuous
    /// relative to one anchor but not the other.
    /// - The specific output of this function should not be considered stable
    ///   across library versions. The only guarantees the library provides are
    ///   that the line length will be `GridDistance(start, end) + 1` and that
    ///   every index in the line will be a neighbor of the preceding index.
    /// - Lines are drawn in grid space, and may not correspond exactly to either
    ///   Cartesian lines or great arcs.
    /// </remarks>
    /// <param name="origin">start index of the line</param>
    /// <param name="destination">end index of the line</param>
    /// <returns>all points from start to end, inclusive; empty if could not
    /// compute a line</returns>
    public static IEnumerable<H3Index> GridPathCells(this H3Index origin, H3Index destination) {
        try {
            return Interpolate(origin, destination);
        } catch {
            // retry interpolation anchored at the destination and reverse the
            // output; this can resolve cases where the local IJK chart is
            // discontinuous relative to one anchor but not the other
            try {
                var path = Interpolate(destination, origin);
                path.Reverse();
                return path;
            } catch {
                return Enumerable.Empty<H3Index>();
            }
        }
    }

    private static List<H3Index> Interpolate(H3Index origin, H3Index destination) {
        // translate to local coordinates
        var startIjk = LocalCoordIJK.ToLocalIJK(origin, origin);
        var endIjk = LocalCoordIJK.ToLocalIJK(origin, destination);

        // get grid distance between start/end
        var distance = startIjk.GetDistanceTo(endIjk);

        // Convert IJK to cube coordinates suitable for linear interpolation
        startIjk.Cube();
        endIjk.Cube();

        double d = distance;
        var iStep = distance > 0 ? (endIjk.I - startIjk.I) / d : 0.0;
        var jStep = distance > 0 ? (endIjk.J - startIjk.J) / d : 0.0;
        var kStep = distance > 0 ? (endIjk.K - startIjk.K) / d : 0.0;

        double startI = startIjk.I;
        double startJ = startIjk.J;
        double startK = startIjk.K;

        List<H3Index> path = new(distance + 1);

        for (var n = 0; n < distance + 1; n += 1) {
            var rounded = CoordIJK.CubeRound(
                startI + iStep * n,
                startJ + jStep * n,
                startK + kStep * n
            );
            rounded.Uncube();
            path.Add(LocalCoordIJK.ToH3Index(origin, rounded));
        }

        return path;
    }

    // ------------------------------------------------------------------
    // Zero-allocation span / buffer-fill overloads (additive; the streaming
    // IEnumerable API above is unchanged).
    // ------------------------------------------------------------------

    /// <summary>
    /// The number of cells in the grid path from <paramref name="origin"/> to
    /// <paramref name="destination"/> (inclusive), i.e. the minimum length of the
    /// destination buffer for the <see cref="Span{T}"/> overload of
    /// <see cref="GridPathCells(H3Index,H3Index,Span{H3Index})"/>.  Equal to
    /// <c>GridDistance + 1</c>; returns <c>-1</c> when the distance cannot be
    /// computed (mirrors <see cref="GridDistance"/>).
    /// </summary>
    /// <param name="origin">start index of the line</param>
    /// <param name="destination">end index of the line</param>
    /// <returns>path length in cells, or -1 if it could not be computed</returns>
    public static int GridPathCellsSize(this H3Index origin, H3Index destination) {
        var distance = origin.GridDistance(destination);
        if (distance >= 0) return distance + 1;

        // GridDistance anchors on the origin's local chart; the path itself may
        // still be computable from the destination's chart (see GridPathCells),
        // so fall back to that anchor for the length.
        var reverse = destination.GridDistance(origin);
        return reverse < 0 ? -1 : reverse + 1;
    }

    /// <summary>
    /// Fills <paramref name="destination"/> with the path of cells between
    /// <paramref name="origin"/> and <paramref name="destination"/> parameter
    /// cells (inclusive) and returns the number of cells written.  Allocation-free
    /// equivalent of the streaming <see cref="GridPathCells(H3Index,H3Index)"/>,
    /// with identical cells and ordering.  Returns <c>0</c> when a line could not
    /// be computed.
    /// </summary>
    /// <param name="origin">start index of the line</param>
    /// <param name="destinationCell">end index of the line</param>
    /// <param name="destination">buffer of at least
    /// <see cref="GridPathCellsSize"/>(<paramref name="origin"/>,
    /// <paramref name="destinationCell"/>) cells</param>
    /// <returns>the number of cells written to <paramref name="destination"/></returns>
    /// <exception cref="ArgumentException">Thrown when the destination buffer is
    /// smaller than <see cref="GridPathCellsSize"/> for a computable line.</exception>
    public static int GridPathCells(this H3Index origin, H3Index destinationCell, Span<H3Index> destination) {
        // Fill anchored on the origin.  Grid distance (the path length) is
        // computed once, inside the fill, because the interpolation needs it
        // anyway — no separate size pass.  If the origin's local IJK chart is
        // discontinuous, retry anchored on the destination and reverse in place;
        // this resolves cases where the chart is discontinuous relative to one
        // anchor but not the other.  A buffer that is too small throws (it is
        // not a chart failure and must not be swallowed by the retry).
        if (TryInterpolateInto(origin, destinationCell, destination, out var count)) {
            return count;
        }

        if (TryInterpolateInto(destinationCell, origin, destination, out count)) {
            destination[..count].Reverse();
            return count;
        }

        return 0;
    }

    /// <summary>
    /// Span-filling core mirroring <see cref="Interpolate"/>, writing the path
    /// into <paramref name="path"/> and returning <c>true</c> with its length in
    /// <paramref name="count"/>.  Returns <c>false</c> only when the local IJK
    /// chart is discontinuous relative to <paramref name="origin"/> (the caller
    /// then retries the other anchor); a destination buffer smaller than the
    /// computed path length throws <see cref="ArgumentException"/> — that is a
    /// caller sizing error, not a chart failure, so it is never swallowed.
    /// </summary>
    private static bool TryInterpolateInto(H3Index origin, H3Index destination, Span<H3Index> path, out int count) {
        CoordIJK startIjk, endIjk;
        int distance;
        try {
            // translate to local coordinates and measure the grid distance
            startIjk = LocalCoordIJK.ToLocalIJK(origin, origin);
            endIjk = LocalCoordIJK.ToLocalIJK(origin, destination);
            distance = startIjk.GetDistanceTo(endIjk);
        } catch {
            count = 0;
            return false;
        }

        if (distance < 0) {
            count = 0;
            return false;
        }

        if (path.Length < distance + 1) {
            throw new ArgumentException(
                $"destination must hold at least {distance + 1} cells (see {nameof(GridPathCellsSize)})", nameof(path));
        }

        // Convert IJK to cube coordinates suitable for linear interpolation
        startIjk.Cube();
        endIjk.Cube();

        double d = distance;
        var iStep = distance > 0 ? (endIjk.I - startIjk.I) / d : 0.0;
        var jStep = distance > 0 ? (endIjk.J - startIjk.J) / d : 0.0;
        var kStep = distance > 0 ? (endIjk.K - startIjk.K) / d : 0.0;

        double startI = startIjk.I;
        double startJ = startIjk.J;
        double startK = startIjk.K;

        for (var n = 0; n < distance + 1; n += 1) {
            var rounded = CoordIJK.CubeRound(
                startI + iStep * n,
                startJ + jStep * n,
                startK + kStep * n
            );
            rounded.Uncube();
            path[n] = LocalCoordIJK.ToH3Index(origin, rounded);
        }

        count = distance + 1;
        return true;
    }

}