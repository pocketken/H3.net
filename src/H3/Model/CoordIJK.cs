using System;
using System.Collections.Generic;
using static H3.Constants;

#nullable enable

namespace H3.Model {

    public class CoordIJK {

        public int I { get; set; } = 0;
        public int J { get; set; } = 0;
        public int K { get; set; } = 0;
        public bool IsValid => this != InvalidIJKCoordinate;

        public static readonly CoordIJK InvalidIJKCoordinate = new(-int.MaxValue, -int.MaxValue, -int.MaxValue);

        public CoordIJK() { }

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

        public static CoordIJK FromVec2d(Vec2d v) {
            CoordIJK h = new();
            int m2;

            // quantize into the ij system and then normalize
            var a1 = Math.Abs(v.X);
            var a2 = Math.Abs(v.Y);

            // first do a reverse conversion
            var x2 = a2 / M_SIN60;
            var x1 = a1 + x2 / 2.0;

            // check if we have the center of a hex
            var m1 = (int)x1;
            m2 = (int)x2;

            // otherwise round correctly
            var r1 = x1 - m1;
            var r2 = x2 - m2;

            switch (r1) {
                case < 0.5 and < 1.0 / 3.0: {
                    if (r2 < (1.0 + r1) / 2.0) {
                        h.I = m1;
                        h.J = m2;
                    } else {
                        h.I = m1;
                        h.J = m2 + 1;
                    }

                    break;
                }

                case < 0.5: {
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

                case < 2.0 / 3.0: {
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

                default: {
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
            if (v.X < 0.0) {
                if (h.J % 2 == 0) // even
                {
                    long axisi = h.J / 2;
                    long diff = h.I - axisi;
                    h.I = (int)(h.I - 2.0 * diff);
                } else {
                    long axisi = (h.J + 1) / 2;
                    long diff = h.I - axisi;
                    h.I = (int)(h.I - (2.0 * diff + 1));
                }
            }

            if (!(v.Y < 0.0))
                return h.Normalize();

            h.I -= (2 * h.J + 1) / 2;
            h.J *= -1;

            return h.Normalize();
        }

        /// <summary>
        /// Normalizes ijk coordinates by setting the components to the smallest possible
        /// values.  Works in place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK Normalize() {
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
            int min = I;

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

        /// <summary>
        /// Rotates ijk coordinates 60 degrees counter-clockwise.  Works in place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK RotateCounterClockwise() {
            var uVecI = LookupTables.DirectionToUnitVector[Direction.IJ];
            var uVecJ = LookupTables.DirectionToUnitVector[Direction.JK];
            var uVecK = LookupTables.DirectionToUnitVector[Direction.IK];

            int i = I * uVecI.I + J * uVecJ.I + K * uVecK.I;
            int j = I * uVecI.J + J * uVecJ.J + K * uVecK.J;
            int k = I * uVecI.K + J * uVecJ.K + K * uVecK.K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }

        /// <summary>
        /// Rotates ijk coordinates 60 degrees clockwise.  Works in place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK RotateClockwise() {
            var uVecI = LookupTables.DirectionToUnitVector[Direction.IK];
            var uVecJ = LookupTables.DirectionToUnitVector[Direction.IJ];
            var uVecK = LookupTables.DirectionToUnitVector[Direction.JK];

            int i = I * uVecI.I + J * uVecJ.I + K * uVecK.I;
            int j = I * uVecI.J + J * uVecJ.J + K * uVecK.J;
            int k = I * uVecI.K + J * uVecJ.K + K * uVecK.K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }

        /// <summary>
        /// Find the normalized ijk coordinates of the indexing parent of a cell in a
        /// counter-clockwise aperture 7 grid.  Works in place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK UpAperature7CounterClockwise() {
            int i = I - K;
            int j = J - K;

            I = (int)Math.Round((3 * i - j) / 7.0, MidpointRounding.AwayFromZero);
            J = (int)Math.Round((i + 2 * j) / 7.0, MidpointRounding.AwayFromZero);
            K = 0;

            return Normalize();
        }

        /// <summary>
        /// Find the normalized ijk coordinates of the indexing parent of a cell in a
        /// clockwise aperture 7 grid.  Works in place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK UpAperature7Clockwise() {
            int i = I - K;
            int j = J - K;

            I = (int)Math.Round((2 * i + j) / 7.0, MidpointRounding.AwayFromZero);
            J = (int)Math.Round((3 * j - i) / 7.0, MidpointRounding.AwayFromZero);
            K = 0;

            return Normalize();
        }

        /// <summary>
        /// Find the normalized ijk coordinates of the hex centered on the indicated
        /// hex at the next finer aperture 7 counter-clockwise resolution.  Works in
        /// place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK DownAperature7CounterClockwise() {
            int i = 3 * I + J;
            int j = 3 * J + K;
            int k = I + 3 * K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }

        /// <summary>
        /// Find the normalized ijk coordinates of the hex centered on the indicated
        /// hex at the next finer aperture 7 clockwise resolution.  Works in place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK DownAperature7Clockwise() {
            int i = 3 * I + K;
            int j = I + 3 * J;
            int k = J + 3 * K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }

        /// <summary>
        /// Find the normalized ijk coordinates of the hex centered on the indicated
        /// hex at the next finer aperture 3 counter-clockwise resolution.  Works in
        /// place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK DownAperature3CounterClockwise() {
            int i = 2 * I + J;
            int j = 2 * J + K;
            int k = I + 2 * K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }

        /// <summary>
        /// Find the normalized ijk coordinates of the hex centered on the indicated
        /// hex at the next finer aperture 3 clockwise resolution.  Works in place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK DownAperature3Clockwise() {
            int i = 2 * I + K;
            int j = I + 2 * J;
            int k = J + 2 * K;

            I = i;
            J = j;
            K = k;

            return Normalize();
        }

        /// <summary>
        /// Convert IJK coordinates to cube coordinates, in place.
        /// </summary>
        /// <returns></returns>
        public CoordIJK Cube() {
            I = -I + K;
            J -= K;
            K = -I - J;
            return this;
        }

        /// <summary>
        /// Convert cube coordinates to IJK coordinates, in place
        /// </summary>
        /// <returns></returns>
        public CoordIJK Uncube() {
            I = -I;
            K = 0;
            return Normalize();
        }

        /// <summary>
        /// Find the normalized ijk coordinates of the hex in the specified digit
        /// direction from the specified ijk coordinates.  Works in place.
        /// </summary>
        /// <param name="direction">The digit direction from the original ijk coordinates.</param>
        /// <returns></returns>
        public CoordIJK ToNeighbour(Direction direction) {
            if (direction is <= Direction.Center or >= Direction.Invalid)
                return this;

            var unitVector = LookupTables.DirectionToUnitVector[direction];
            I += unitVector.I;
            J += unitVector.J;
            K += unitVector.K;
            Normalize();
            return this;
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
            int i = I - K;
            int j = J - K;

            return new Vec2d {
                X = i - 0.5 * j,
                Y = j * M_SQRT3_2
            };
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

        public static CoordIJK UpAperature7CounterClockwise(CoordIJK source) =>
            new CoordIJK(source).UpAperature7CounterClockwise();

        public static CoordIJK UpAperatureClockwise(CoordIJK source) =>
            new CoordIJK(source).UpAperature7Clockwise();

        public static CoordIJK DownAperature7CounterClockwise(CoordIJK source) =>
            new CoordIJK(source).DownAperature7CounterClockwise();

        public static CoordIJK DownAperatureClockwise(CoordIJK source) =>
            new CoordIJK(source).DownAperature7Clockwise();

        public static CoordIJK DownAperature3CounterClockwise(CoordIJK source) =>
            new CoordIJK(source).DownAperature3CounterClockwise();

        public static CoordIJK DownAperature3Clockwise(CoordIJK source) =>
            new CoordIJK(source).DownAperature3Clockwise();

        /// <summary>
        /// Given cube coords as doubles, round to valid integer coordinates. Algorithm
        /// from https://www.redblobgames.com/grids/hexagons/#rounding
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static CoordIJK CubeRound(double i, double j, double k) {
            CoordIJK coord = new(
                (int)Math.Round(i, MidpointRounding.AwayFromZero),
                (int)Math.Round(j, MidpointRounding.AwayFromZero),
                (int)Math.Round(k, MidpointRounding.AwayFromZero)
            );

            double iDiff = Math.Abs(coord.I - i);
            double jDiff = Math.Abs(coord.J - j);
            double kDiff = Math.Abs(coord.K - k);

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
        public static implicit operator Direction(CoordIJK h) =>
            LookupTables.UnitVectorToDirection.GetValueOrDefault(Normalize(h), Direction.Invalid);

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

        public static bool operator ==(CoordIJK? a, CoordIJK? b) =>
            a?.I == b?.I && a?.J == b?.J && a?.K == b?.K;

        public static bool operator !=(CoordIJK? a, CoordIJK? b) =>
            a?.I != b?.I || a?.J != b?.J || a?.K != b?.K;

        public override bool Equals(object? other) =>
            other is CoordIJK c && I == c.I && J == c.J && K == c.K;

        public override string ToString() {
            return $"({I}, {J}, {K})";
        }

        public override int GetHashCode() => HashCode.Combine(I, J, K);

    }

}