using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static H3.Constants;

#nullable enable

namespace H3.Model;

// TODO sealed class => struct

public sealed class CoordIJK {

    public int I { get; set; }
    public int J { get; set; }
    public int K { get; set; }
    public bool IsValid => this != InvalidIJKCoordinate;

    public static readonly CoordIJK InvalidIJKCoordinate = new(-int.MaxValue, -int.MaxValue, -int.MaxValue);

    public CoordIJK() {
    }

    public CoordIJK(int i, int j, int k) {
        I = i;
        J = j;
        K = k;
    }

    public CoordIJK(CoordIJK source) {
        I = source.I;
        J = source.J;
        K = source.K;
    }

    public static CoordIJK FromVec2d(double x, double y, CoordIJK? destination = default) {
        unchecked {
            var h = destination ?? new CoordIJK();

            // quantize into the ij system and then normalize
            var a1 = Math.Abs(x);
            var a2 = Math.Abs(y);

            // first do a reverse conversion
            var x2 = a2 / M_SIN60;
            var x1 = a1 + x2 / 2.0;

            // check if we have the center of a hex
            var m1 = (int)x1;
            var m2 = (int)x2;

            // otherwise round correctly
            var r1 = x1 - m1;
            var r2 = x2 - m2;

            switch (r1) {
                case < 0.3333333333333333:
                {
                    if (r2 < (1.0 + r1) / 2.0) {
                        h.I = m1;
                        h.J = m2;
                    } else {
                        h.I = m1;
                        h.J = m2 + 1;
                    }

                    break;
                }

                case < 0.5:
                {
                    if (r2 < 1.0 - r1) {
                        h.J = m2;
                    } else {
                        h.J = m2 + 1;
                    }

                    if (1.0 - r1 <= r2 && r2 < 2.0 * r1) {
                        h.I = m1 + 1;
                    } else {
                        h.I = m1;
                    }

                    break;
                }

                case < 0.6666666666666666:
                {
                    if (r2 < 1.0 - r1) {
                        h.J = m2;
                    } else {
                        h.J = m2 + 1;
                    }

                    if (2.0 * r1 - 1.0 < r2 && r2 < 1.0 - r1) {
                        h.I = m1;
                    } else {
                        h.I = m1 + 1;
                    }

                    break;
                }

                default:
                {
                    if (r2 < r1 / 2.0) {
                        h.I = m1 + 1;
                        h.J = m2;
                    } else {
                        h.I = m1 + 1;
                        h.J = m2 + 1;
                    }

                    break;
                }
            }

            // now fold across the axes if necessary
            if (x < 0.0) {
                if (h.J % 2 == 0) {
                    // even
                    long axisI = h.J / 2;
                    var diff = h.I - axisI;
                    h.I = (int)(h.I - 2.0 * diff);
                } else {
                    long axisI = (h.J + 1) / 2;
                    var diff = h.I - axisI;
                    h.I = (int)(h.I - (2.0 * diff + 1));
                }
            }

            if (y >= 0.0)
                return h.Normalize();

            h.I -= (2 * h.J + 1) / 2;
            h.J *= -1;

            return h.Normalize();
        }
    }

    /// <summary>
    /// Normalizes ijk coordinates by setting the components to the smallest possible
    /// values.  Works in place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK Normalize() {
        unchecked {
            // remove any negative values
            if (I < 0) {
                J -= I;
                K -= I;
                I = 0;
            }

            if (J < 0) {
                I -= J;
                K -= J;
                J = 0;
            }

            if (K < 0) {
                I -= K;
                J -= K;
                K = 0;
            }

            // remove the min value if needed
            var min = I;

            if (J < min)
                min = J;

            if (K < min)
                min = K;

            if (min <= 0)
                return this;

            I -= min;
            J -= min;
            K -= min;

            return this;
        }
    }

    /// <summary>
    /// Rotates ijk coordinates 60 degrees counter-clockwise.  Works in place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK RotateCounterClockwise() {
        unchecked {
            var i = I + K;
            var j = I + J;
            var k = J + K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }
    }

    /// <summary>
    /// Rotates ijk coordinates 60 degrees clockwise.  Works in place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK RotateClockwise() {
        unchecked {
            var i = I + J;
            var j = J + K;
            var k = I + K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }
    }

    /// <summary>
    /// Find the normalized ijk coordinates of the indexing parent of a cell in a
    /// counter-clockwise aperture 7 grid.  Works in place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK UpAperture7CounterClockwise() {
        unchecked {
            var i = I - K;
            var j = J - K;

            I = (int)Utils.CRound((3 * i - j) / 7.0);
            J = (int)Utils.CRound((i + 2 * j) / 7.0);
            K = 0;

            return Normalize();
        }
    }

    /// <summary>
    /// Find the normalized ijk coordinates of the indexing parent of a cell in a
    /// clockwise aperture 7 grid.  Works in place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK UpAperture7Clockwise() {
        unchecked {
            var i = I - K;
            var j = J - K;

            I = (int)Utils.CRound((2 * i + j) / 7.0);
            J = (int)Utils.CRound((3 * j - i) / 7.0);
            K = 0;

            return Normalize();
        }
    }

    /// <summary>
    /// Find the normalized ijk coordinates of the hex centered on the indicated
    /// hex at the next finer aperture 7 counter-clockwise resolution.  Works in
    /// place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK DownAperture7CounterClockwise() {
        unchecked {
            var i = 3 * I + J;
            var j = 3 * J + K;
            var k = I + 3 * K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }
    }

    /// <summary>
    /// Find the normalized ijk coordinates of the hex centered on the indicated
    /// hex at the next finer aperture 7 clockwise resolution.  Works in place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK DownAperture7Clockwise() {
        unchecked {
            var i = 3 * I + K;
            var j = I + 3 * J;
            var k = J + 3 * K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }
    }

    /// <summary>
    /// Find the normalized ijk coordinates of the hex centered on the indicated
    /// hex at the next finer aperture 3 counter-clockwise resolution.  Works in
    /// place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK DownAperture3CounterClockwise() {
        unchecked {
            var i = 2 * I + J;
            var j = 2 * J + K;
            var k = I + 2 * K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }
    }

    /// <summary>
    /// Find the normalized ijk coordinates of the hex centered on the indicated
    /// hex at the next finer aperture 3 clockwise resolution.  Works in place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK DownAperture3Clockwise() {
        unchecked {
            var i = 2 * I + K;
            var j = I + 2 * J;
            var k = J + 2 * K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }
    }

    /// <summary>
    /// Convert IJK coordinates to cube coordinates, in place.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK Cube() {
        unchecked {
            I = -I + K;
            J -= K;
            K = -I - J;
            return this;
        }
    }

    /// <summary>
    /// Convert cube coordinates to IJK coordinates, in place
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK Uncube() {
        unchecked {
            I = -I;
            K = 0;
            return Normalize();
        }
    }

    /// <summary>
    /// Find the normalized ijk coordinates of the hex in the specified digit
    /// direction from the specified ijk coordinates.  Works in place.
    /// </summary>
    /// <param name="direction">The digit direction from the original ijk coordinates.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoordIJK ToNeighbour(Direction direction) {
        unchecked {
            if (direction is <= Direction.Center or >= Direction.Invalid)
                return this;

            var unitVector = LookupTables.UnitVectors[(int)direction];
            I += unitVector.I;
            J += unitVector.J;
            K += unitVector.K;
            return Normalize();
        }
    }

    /// <summary>
    /// Finds the grid distance between the two coordinates.
    /// </summary>
    /// <param name="h2"></param>
    /// <returns></returns>
    public int GetDistanceTo(CoordIJK h2) {
        var diff = new CoordIJK(I - h2.I, J - h2.J, K - h2.K).Normalize();
        return Math.Max(Math.Abs(diff.I), Math.Max(Math.Abs(diff.J), Math.Abs(diff.K)));
    }

    public Vec2d ToVec2d() {
        return new Vec2d(GetVec2dOrdinates());
    }

    public Vec2d ToVec2d(ref Vec2d toUpdate) {
        var (x, y) = GetVec2dOrdinates();
        toUpdate.X = x;
        toUpdate.Y = y;
        return toUpdate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (double, double) GetVec2dOrdinates() {
        unchecked {
            var i = I - K;
            var j = J - K;

            return (i - 0.5 * j, j * M_SQRT3_2);
        }
    }

    public CoordIJ ToCoordIJ() => CoordIJ.FromCoordIJK(this);

    public static CoordIJK Cube(CoordIJK source) =>
        new CoordIJK(source).Cube();

    public static CoordIJK Uncube(CoordIJK source) =>
        new CoordIJK(source).Uncube();

    public static CoordIJK Normalize(CoordIJK source) =>
        new CoordIJK(source).Normalize();

    public static CoordIJK ToNeighbour(CoordIJK source, Direction direction) =>
        new CoordIJK(source).ToNeighbour(direction);

    public static CoordIJK RotateCounterClockwise(CoordIJK source) =>
        new CoordIJK(source).RotateCounterClockwise();

    public static CoordIJK RotateClockwise(CoordIJK source) =>
        new CoordIJK(source).RotateClockwise();

    public static CoordIJK UpAperture7CounterClockwise(CoordIJK source) =>
        new CoordIJK(source).UpAperture7CounterClockwise();

    public static CoordIJK UpApertureClockwise(CoordIJK source) =>
        new CoordIJK(source).UpAperture7Clockwise();

    public static CoordIJK DownAperture7CounterClockwise(CoordIJK source) =>
        new CoordIJK(source).DownAperture7CounterClockwise();

    public static CoordIJK DownApertureClockwise(CoordIJK source) =>
        new CoordIJK(source).DownAperture7Clockwise();

    public static CoordIJK DownAperture3CounterClockwise(CoordIJK source) =>
        new CoordIJK(source).DownAperture3CounterClockwise();

    public static CoordIJK DownAperture3Clockwise(CoordIJK source) =>
        new CoordIJK(source).DownAperture3Clockwise();

    /// <summary>
    /// Given cube coords as doubles, round to valid integer coordinates. Algorithm
    /// from https://www.redblobgames.com/grids/hexagons/#rounding
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <param name="k"></param>
    /// <param name="toUpdate">optional instance to update, returns a new
    /// <see cref="CoordIJK"/> instance if not provided.</param>
    /// <returns></returns>
    public static CoordIJK CubeRound(double i, double j, double k, CoordIJK? toUpdate = default) {
        var coord = toUpdate ?? new CoordIJK();

        coord.I = (int)Utils.CRound(i);
        coord.J = (int)Utils.CRound(j);
        coord.K = (int)Utils.CRound(k);

        var iDiff = Math.Abs(coord.I - i);
        var jDiff = Math.Abs(coord.J - j);
        var kDiff = Math.Abs(coord.K - k);

        // Round, maintaining valid cube coords
        if (iDiff > jDiff && iDiff > kDiff) {
            coord.I = -coord.J - coord.K;
        } else if (jDiff > kDiff) {
            coord.J = -coord.I - coord.K;
        } else {
            coord.K = -coord.I - coord.J;
        }

        return coord;
    }

    /// <summary>
    /// Creates a new CoordIJK from a tuple containing 3 integers (I, J, K).
    /// </summary>
    /// <param name="coordinates"></param>
    public static implicit operator CoordIJK((int, int, int) coordinates) =>
        new(coordinates.Item1, coordinates.Item2, coordinates.Item3);

    /// <summary>
    /// Determines the H3 digit corresponding to a unit vector in ijk coordinates.
    /// </summary>
    /// <param name="h"></param>
#if NETSTANDARD2_0
    public static implicit operator Direction(CoordIJK h) {
        var unitVector = Normalize(h);
        if (!LookupTables.UnitVectorToDirection.ContainsKey(unitVector)) return Direction.Invalid;
        return LookupTables.UnitVectorToDirection[unitVector];
    }
#else
    public static implicit operator Direction(CoordIJK h) =>
        LookupTables.UnitVectorToDirection.GetValueOrDefault(Normalize(h), Direction.Invalid);
#endif

    /// <summary>
    /// Returns a new ijk coordinate containing the sum of two ijk
    /// coordinates.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static CoordIJK operator +(CoordIJK a, CoordIJK b) {
        return new CoordIJK {
            I = a.I + b.I,
            J = a.J + b.J,
            K = a.K + b.K
        };
    }

    /// <summary>
    /// Returns a new ijk coordinate containing the difference of
    /// two ijk coordinates.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static CoordIJK operator -(CoordIJK a, CoordIJK b) {
        return new CoordIJK {
            I = a.I - b.I,
            J = a.J - b.J,
            K = a.K - b.K
        };
    }

    /// <summary>
    /// Returns a new ijk coordinate that has been scaled by the
    /// specified factor.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public static CoordIJK operator *(CoordIJK a, int factor) {
        return new CoordIJK {
            I = a.I * factor,
            J = a.J * factor,
            K = a.K * factor
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(CoordIJK? a, CoordIJK? b) {
        if (a is null) return b is null;
        if (b is null) return false;
        return a.I == b.I && a.J == b.J && a.K == b.K;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(CoordIJK? a, CoordIJK? b) {
        if (a is null) return b is not null;
        if (b is null) return true;
        return a.I != b.I || a.J != b.J || a.K != b.K;
    }

    public override bool Equals(object? other) =>
        other is CoordIJK c && this == c;

    public override string ToString() {
        return $"({I}, {J}, {K})";
    }

    public override int GetHashCode() => HashCode.Combine(I, J, K);

}