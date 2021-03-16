using System;

#nullable enable

namespace H3.Model {

    public class CoordIJ {
        public int I { get; set; }
        public int J { get; set; }

        public CoordIJ() { }

        public CoordIJ(int i, int j) {
            I = i;
            J = j;
        }

        public CoordIJ(CoordIJ source) {
            I = source.I;
            J = source.J;
        }

        public static CoordIJ FromCoordIJK(CoordIJK ijk) => new CoordIJ {
            I = ijk.I - ijk.K,
            J = ijk.J - ijk.K
        };

        public static implicit operator CoordIJ((int, int) coord) =>
            new CoordIJ(coord.Item1, coord.Item2);

        public CoordIJK ToCoordIJK() => new CoordIJK(I, J, 0).Normalize();

        public static bool operator ==(CoordIJ a, CoordIJ b) => a.I == b.I && a.J == b.J;

        public static bool operator !=(CoordIJ a, CoordIJ b) => a.I != b.I || a.J != b.J;

        public override bool Equals(object? other) => other is CoordIJ c && I == c.I && J == c.J;
        public override string ToString() {
            return $"({I}, {J})";
        }

        public override int GetHashCode() => HashCode.Combine(I, J);
    }

}
