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

        public static readonly CoordIJK InvalidIJKCoordinate = new CoordIJK(-int.MaxValue, -int.MaxValue, -int.MaxValue);

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
            double a1, a2;
            double x1, x2;
            int m1, m2;
            double r1, r2;

            // quantize into the ij system and then normalize
            a1 = Math.Abs(v.X);
            a2 = Math.Abs(v.Y);

            // first do a reverse conversion
            x2 = a2 / M_SIN60;
            x1 = a1 + x2 / 2.0;

            // check if we have the center of a hex
            m1 = (int)x1;
            m2 = (int)x2;

            // otherwise round correctly
            r1 = x1 - m1;
            r2 = x2 - m2;

            if (r1 < 0.5) {
                if (r1 < 1.0 / 3.0) {
                    if (r2 < (1.0 + r1) / 2.0) {
                        h.I = m1;
                        h.J = m2;
                    } else {
                        h.I = m1;
                        h.J = m2 + 1;
                    }
                } else {
                    if (r2 < (1.0 - r1)) {
                        h.J = m2;
                    } else {
                        h.J = m2 + 1;
                    }

                    if ((1.0 - r1) <= r2 && r2 < (2.0 * r1)) {
                        h.I = m1 + 1;
                    } else {
                        h.I = m1;
                    }
                }
            } else {
                if (r1 < 2.0 / 3.0) {
                    if (r2 < (1.0 - r1)) {
                        h.J = m2;
                    } else {
                        h.J = m2 + 1;
                    }

                    if ((2.0 * r1 - 1.0) < r2 && r2 < (1.0 - r1)) {
                        h.I = m1;
                    } else {
                        h.I = m1 + 1;
                    }
                } else {
                    if (r2 < (r1 / 2.0)) {
                        h.I = m1 + 1;
                        h.J = m2;
                    } else {
                        h.I = m1 + 1;
                        h.J = m2 + 1;
                    }
                }
            }

            // now fold across the axes if necessary
            if (v.X < 0.0) {
                if ((h.J % 2) == 0)  // even
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

            if (v.Y < 0.0) {
                h.I -= (2 * h.J + 1) / 2;
                h.J *= -1;
            }

            return h.Normalize();
        }

        private CoordIJK SetFrom(CoordIJK other) {
            I = other.I;
            J = other.J;
            K = other.K;
            return this;
        }

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

            if (min > 0) {
                I -= min;
                J -= min;
                K -= min;
            }

            return this;
        }

        public CoordIJK RotateCounterClockwise() {
            CoordIJK iVec = LookupTables.IndexToUnitVector[CellIndex.IJ] * I;
            CoordIJK jVec = LookupTables.IndexToUnitVector[CellIndex.JK] * J;
            CoordIJK kVec = LookupTables.IndexToUnitVector[CellIndex.IK] * K;

            return SetFrom(iVec + jVec + kVec).Normalize();
        }

        public CoordIJK RotateClockwise() {
            CoordIJK iVec = LookupTables.IndexToUnitVector[CellIndex.IK] * I;
            CoordIJK jVec = LookupTables.IndexToUnitVector[CellIndex.IJ] * J;
            CoordIJK kVec = LookupTables.IndexToUnitVector[CellIndex.JK] * K;

            return SetFrom(iVec + jVec + kVec).Normalize();
        }

        public CoordIJK UpAperature7CounterClockwise() {
            int i = I - K;
            int j = J - K;

            I = (int)Math.Round((3 * i - j) / 7.0);
            J = (int)Math.Round((i + 2 * j) / 7.0);
            K = 0;

            return Normalize();
        }

        public CoordIJK UpAperature7Clockwise() {
            int i = I - K;
            int j = J - K;

            I = (int)Math.Round((2 * i + j) / 7.0);
            J = (int)Math.Round((3 * j - i) / 7.0);
            K = 0;

            return Normalize();
        }

        public CoordIJK DownAperature7CounterClockwise() {
            CoordIJK iVec = new CoordIJK(3, 0, 1) * I;
            CoordIJK jVec = new CoordIJK(1, 3, 0) * J;
            CoordIJK kVec = new CoordIJK(0, 1, 3) * K;

            return SetFrom(iVec + jVec + kVec).Normalize();
        }

        public CoordIJK DownAperature7Clockwise() {
            CoordIJK iVec = new CoordIJK(3, 1, 0) * I;
            CoordIJK jVec = new CoordIJK(0, 3, 1) * J;
            CoordIJK kVec = new CoordIJK(1, 0, 3) * K;

            return SetFrom(iVec + jVec + kVec).Normalize();
        }

        public CoordIJK DownAperature3CounterClockwise() {
            CoordIJK iVec = new CoordIJK(2, 0, 1) * I;
            CoordIJK jVec = new CoordIJK(1, 2, 0) * J;
            CoordIJK kVec = new CoordIJK(0, 1, 2) * K;

            return SetFrom(iVec + jVec + kVec).Normalize();
        }

        public CoordIJK DownAperature3Clockwise() {
            CoordIJK iVec = new CoordIJK(2, 1, 0) * I;
            CoordIJK jVec = new CoordIJK(0, 2, 1) * J;
            CoordIJK kVec = new CoordIJK(1, 0, 2) * K;

            return SetFrom(iVec + jVec + kVec).Normalize();
        }

        public CoordIJK Cube() {
            I += K;
            J -= K;
            K -= J;
            return this;
        }

        public CoordIJK Uncube() {
            I -= -I;
            K = 0;
            return Normalize();
        }

        public CoordIJK ToNeighbour(CellIndex cellIndex) {
            if (cellIndex > CellIndex.Center && cellIndex < CellIndex.Invalid) {
                SetFrom(this + LookupTables.IndexToUnitVector[cellIndex]).Normalize();
            }
            return this;
        }

        public int GetDistanceTo(CoordIJK h2) {
            var diff = (this - h2).Normalize();
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

        public static CoordIJK ToNeighbour(CoordIJK source, CellIndex direction) =>
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

        public static implicit operator CellIndex(CoordIJK h) =>
            LookupTables.UnitVectorToIndex.GetValueOrDefault(Normalize(h), CellIndex.Invalid);

        public static CoordIJK operator +(CoordIJK a, CoordIJK b) {
            return new CoordIJK {
                I = a.I + b.I,
                J = a.J + b.J,
                K = a.K + b.K
            };
        }

        public static CoordIJK operator -(CoordIJK a, CoordIJK b) {
            return new CoordIJK {
                I = a.I - b.I,
                J = a.J - b.J,
                K = a.K - b.K
            };
        }

        public static CoordIJK operator *(CoordIJK a, int factor) {
            return new CoordIJK {
                I = a.I * factor,
                J = a.J * factor,
                K = a.K * factor
            };
        }

        public override bool Equals(object? other) => other is CoordIJK c && I == c.I && J == c.J && K == c.K;

        public override int GetHashCode() => HashCode.Combine(I, J, K);
    }

}
