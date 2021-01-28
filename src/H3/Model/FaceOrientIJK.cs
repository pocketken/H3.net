using System;
using static H3.Constants;

#nullable enable

namespace H3.Model {
    public class FaceOrientIJK {
        public int Face { get; set; }
        public CoordIJK Translate { get; set; } = new CoordIJK();
        public int CounterClockwiseRotations { get; set; }

        public FaceOrientIJK() { }

        public FaceOrientIJK(int face, CoordIJK translate, int rotation) {
            Face = face;
            Translate = translate;
            CounterClockwiseRotations = rotation;
        }

        public FaceOrientIJK(int face, int i, int j, int k, int rotation) {
            Face = face;
            Translate = new CoordIJK(i, j, k);
            CounterClockwiseRotations = rotation;
        }

        public override bool Equals(object? other) {
            return other is FaceOrientIJK f && Face == f.Face && Translate == f.Translate &&
                CounterClockwiseRotations == f.CounterClockwiseRotations;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Face, Translate, CounterClockwiseRotations);
        }
    }
}
