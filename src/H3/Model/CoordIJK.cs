using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static H3.Constants;

#nullable enable

namespace H3.Model;

public struct CoordIJK : IEquatable<CoordIJK> {

    public int I { get; set; }
    public int J { get; set; }
    public int K { get; set; }
    public readonly bool IsValid => this != InvalidIJKCoordinate;

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

    public static CoordIJK FromVec2d(double x, double y) {
        unchecked {
            var h = new CoordIJK();

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
    /// Whether or not normalization of the provided i and j components (with a
    /// zero k component) could produce intermediate values that overflow 32 bit
    /// signed integers.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool NormalizeCouldOverflow(int i, int j) {
        long max = Math.Max(i, j);
        long min = Math.Min(i, j);

        if (min >= 0) return false;

        var sum = max + min;
        return sum > int.MaxValue || sum < int.MinValue || -min > int.MaxValue || max - min > int.MaxValue;
    }

    /// <summary>
    /// Rounds <paramref name="n"/>/7 to the nearest integer, ties away from zero,
    /// using only integer arithmetic.  This is bit-for-bit identical to
    /// <c>(long)Utils.CRound(n / 7.0)</c> for every value produced on the
    /// aperture-7 up-scaling paths: for an integer <paramref name="n"/> the exact
    /// quotient n/7 is never a half-integer (its fractional part is a multiple of
    /// 1/7, at least 0.14 away from 0.5), so round-half-away equals
    /// round-to-nearest; and because all operands here derive from 32-bit ijk
    /// components (|n| &lt; 2^34 &lt;&lt; 2^52), n/7.0 is the correctly-rounded double
    /// quotient and <c>Floor(n/7.0 + 0.5)</c> / <c>Ceiling(n/7.0 - 0.5)</c> land on
    /// the same integer as this.  The constant divisor is strength-reduced by the
    /// JIT to a multiply+shift, removing the double divide and the Floor/Ceiling of
    /// <see cref="Utils.CRound"/> from the serially dependent up-aperture chain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long RoundDiv7(long n) => (n + (n >= 0 ? 3L : -3L)) / 7;

    /// <summary>
    /// Find the normalized ijk coordinates of the indexing parent of a cell in a
    /// counter-clockwise aperture 7 grid, validating that the operation does not
    /// overflow.  Works in place.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="OverflowException">Thrown when the input coordinates are
    /// too large to be transformed without overflowing.</exception>
    internal CoordIJK UpAperture7CounterClockwiseChecked() {
        var i = (long)I - K;
        var j = (long)J - K;

        var newI = RoundDiv7(3 * i - j);
        var newJ = RoundDiv7(i + 2 * j);

        if (newI is > int.MaxValue or < int.MinValue || newJ is > int.MaxValue or < int.MinValue || NormalizeCouldOverflow((int)newI, (int)newJ)) {
            throw new OverflowException("ijk coordinates would overflow");
        }

        I = (int)newI;
        J = (int)newJ;
        K = 0;

        return Normalize();
    }

    /// <summary>
    /// Find the normalized ijk coordinates of the indexing parent of a cell in a
    /// clockwise aperture 7 grid, validating that the operation does not
    /// overflow.  Works in place.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="OverflowException">Thrown when the input coordinates are
    /// too large to be transformed without overflowing.</exception>
    internal CoordIJK UpAperture7ClockwiseChecked() {
        var i = (long)I - K;
        var j = (long)J - K;

        var newI = RoundDiv7(2 * i + j);
        var newJ = RoundDiv7(3 * j - i);

        if (newI is > int.MaxValue or < int.MinValue || newJ is > int.MaxValue or < int.MinValue || NormalizeCouldOverflow((int)newI, (int)newJ)) {
            throw new OverflowException("ijk coordinates would overflow");
        }

        I = (int)newI;
        J = (int)newJ;
        K = 0;

        return Normalize();
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

            I = (int)RoundDiv7(3 * i - j);
            J = (int)RoundDiv7(i + 2 * j);
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

            I = (int)RoundDiv7(2 * i + j);
            J = (int)RoundDiv7(3 * j - i);
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
    /// Fused aperture-7 down-step (counter-clockwise when <paramref name="isClass3"/>,
    /// otherwise clockwise) immediately followed by a translation to the neighbour in
    /// the given <paramref name="direction"/>.  Works in place.
    ///
    /// This is bit-for-bit identical to calling
    /// <see cref="DownAperture7CounterClockwise()"/> / <see cref="DownAperture7Clockwise"/>
    /// and then <see cref="ToNeighbour(Direction)"/>, but performs a single normalization rather
    /// than two: <see cref="Normalize()"/> only ever subtracts an integer multiple of
    /// (1,1,1), and is therefore invariant under adding one, so normalizing the summed
    /// (down-stepped + unit-vector) coordinates once yields exactly the same canonical
    /// result as normalizing after each sub-step.  Adding the direction's ijk unit
    /// vector <c>((d&gt;&gt;2)&amp;1, (d&gt;&gt;1)&amp;1, d&amp;1)</c> unconditionally is correct for
    /// every digit value 0..7: Center (0) adds nothing, and Invalid (7) adds (1,1,1),
    /// which the normalization removes — matching <see cref="ToNeighbour(Direction)"/>'s
    /// early-return (no-op after the down-step's own normalize) for those digits.
    /// </summary>
    /// <param name="isClass3">Whether the resolution being descended into is Class III
    /// (counter-clockwise aperture 7); otherwise Class II (clockwise).</param>
    /// <param name="direction">The digit direction to translate to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DownAperture7ToNeighbour(bool isClass3, Direction direction) {
        unchecked {
            int i, j, k;
            if (isClass3) {
                // DownAperture7CounterClockwise
                i = 3 * I + J;
                j = 3 * J + K;
                k = I + 3 * K;
            } else {
                // DownAperture7Clockwise
                i = 3 * I + K;
                j = I + 3 * J;
                k = J + 3 * K;
            }

            // ToNeighbour: add the direction digit's ijk unit vector.
            var d = (int)direction;
            i += (d >> 2) & 1;
            j += (d >> 1) & 1;
            k += d & 1;

            // i, j, k are always >= 0 here (this is only reached from the
            // resolution digit walk, whose inputs — base-cell home coords or
            // (0,0,0) — are non-negative and preserved by every step, and both
            // the aperture-7 transform and the unit vector are non-negative), so
            // the negative-clamp branches of Normalize are provably dead and only
            // the min-subtraction remains.  Bit-for-bit identical to Normalize().
            var min = i < j ? i : j;
            if (k < min) min = k;
            I = i - min;
            J = j - min;
            K = k - min;
        }
    }

    /// <summary>
    /// Fused pair of aperture-7 down-steps for the resolution digit walk: a
    /// Class III (counter-clockwise) step translating to <paramref name="oddDigit"/>
    /// immediately followed by a Class II (clockwise) step translating to
    /// <paramref name="evenDigit"/>, performed with a SINGLE normalization rather
    /// than one per step.  Works in place.
    ///
    /// On the digit walk the resolution parity alternates, so an odd-resolution
    /// digit (Class III, CCW) is always followed by an even-resolution digit
    /// (Class II, CW); this fuses that fixed pair.  Composing the two integer
    /// transforms yields the symmetric map [[10,3,3],[3,10,3],[3,3,10]] plus a
    /// constant offset formed from the two digits' ijk unit vectors.  Deferring
    /// the first step's normalization into the second is bit-for-bit identical to
    /// normalizing after each step: <see cref="Normalize()"/> only ever subtracts a
    /// multiple of (1,1,1), and every aperture-7 down-step maps (m,m,m) to
    /// (4m,4m,4m) — again a multiple of (1,1,1) — so the deferred (1,1,1) offset
    /// is removed by the single trailing normalization (the round-8
    /// <see cref="DownAperture7ToNeighbour"/> identity applied one level up).
    /// Inputs on this path are always non-negative, so — as in
    /// <see cref="DownAperture7ToNeighbour"/> — only the min-subtraction of the
    /// normalization is required.
    /// </summary>
    /// <param name="oddDigit">The Class III (CCW) step's direction digit.</param>
    /// <param name="evenDigit">The Class II (CW) step's direction digit.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DownAperture7ClassIIIThenClassII(Direction oddDigit, Direction evenDigit) {
        unchecked {
            // ijk unit vectors of the two digits: ((d >> 2) & 1, (d >> 1) & 1, d & 1)
            var d1 = (int)oddDigit;
            var d2 = (int)evenDigit;
            var b1i = (d1 >> 2) & 1;
            var b1j = (d1 >> 1) & 1;
            var b1k = d1 & 1;
            var b2i = (d2 >> 2) & 1;
            var b2j = (d2 >> 1) & 1;
            var b2k = d2 & 1;

            // CW(CCW(I,J,K) + u1) + u2, expanded:
            var i = 10 * I + 3 * J + 3 * K + (3 * b1i + b1k + b2i);
            var j = 3 * I + 10 * J + 3 * K + (b1i + 3 * b1j + b2j);
            var k = 3 * I + 3 * J + 10 * K + (b1j + 3 * b1k + b2k);

            // i, j, k are all >= 0, so only the min-subtraction of Normalize is
            // required (the negative-clamp branches are provably dead).
            var min = i < j ? i : j;
            if (k < min) min = k;
            I = i - min;
            J = j - min;
            K = k - min;
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

            // the bits of a valid direction digit are its ijk unit vector,
            // i.e. UnitVectors[d] == ((d >> 2) & 1, (d >> 1) & 1, d & 1)
            var d = (int)direction;
            I += (d >> 2) & 1;
            J += (d >> 1) & 1;
            K += d & 1;
            return Normalize();
        }
    }

    /// <summary>
    /// Finds the grid distance between the two coordinates.
    /// </summary>
    /// <param name="h2"></param>
    /// <returns></returns>
    public readonly int GetDistanceTo(CoordIJK h2) {
        var diff = new CoordIJK(I - h2.I, J - h2.J, K - h2.K).Normalize();
        return Math.Max(Math.Abs(diff.I), Math.Max(Math.Abs(diff.J), Math.Abs(diff.K)));
    }

    public readonly Vec2d ToVec2d() {
        return new Vec2d(GetVec2dOrdinates());
    }

    public readonly Vec2d ToVec2d(ref Vec2d toUpdate) {
        var (x, y) = GetVec2dOrdinates();
        toUpdate.X = x;
        toUpdate.Y = y;
        return toUpdate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly (double, double) GetVec2dOrdinates() {
        unchecked {
            var i = I - K;
            var j = J - K;

            return (i - 0.5 * j, j * M_SQRT3_2);
        }
    }

    public readonly CoordIJ ToCoordIJ() => CoordIJ.FromCoordIJK(this);

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
    /// <returns></returns>
    public static CoordIJK CubeRound(double i, double j, double k) {
        var coord = new CoordIJK();

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
    public static implicit operator Direction(CoordIJK h) {
        var i = h.I;
        var j = h.J;
        var k = h.K;

        // normalize, without mutating the input or allocating a copy
        if (i < 0) {
            j -= i;
            k -= i;
            i = 0;
        }

        if (j < 0) {
            i -= j;
            k -= j;
            j = 0;
        }

        if (k < 0) {
            i -= k;
            j -= k;
            k = 0;
        }

        var min = Math.Min(i, Math.Min(j, k));
        if (min > 0) {
            i -= min;
            j -= min;
            k -= min;
        }

        // the components of a normalized unit vector map directly onto the
        // Direction bits, i.e. (i << 2) | (j << 1) | k
        return (uint)i > 1 || (uint)j > 1 || (uint)k > 1
            ? Direction.Invalid
            : (Direction)((i << 2) | (j << 1) | k);
    }

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
    public readonly bool Equals(CoordIJK other) => I == other.I && J == other.J && K == other.K;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(CoordIJK a, CoordIJK b) =>
        a.I == b.I && a.J == b.J && a.K == b.K;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(CoordIJK a, CoordIJK b) =>
        a.I != b.I || a.J != b.J || a.K != b.K;

    public override readonly bool Equals(object? other) =>
        other is CoordIJK c && this == c;

    public override readonly string ToString() {
        return $"({I}, {J}, {K})";
    }

    public override readonly int GetHashCode() => HashCode.Combine(I, J, K);

}