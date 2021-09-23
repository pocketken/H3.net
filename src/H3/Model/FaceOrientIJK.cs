using System;

#nullable enable

namespace H3.Model {

    public sealed class FaceOrientIJK {
        public int Face { get; init; }
        public CoordIJK Translate { get; init; } = new();
        public int CounterClockwiseRotations { get; init; }

        private FaceOrientIJK() { }

        public FaceOrientIJK(int face, CoordIJK translate, int rotation) {
            Face = face;
            Translate = translate;
            CounterClockwiseRotations = rotation;
        }

        public static implicit operator FaceOrientIJK((int, (int, int, int), int) args) =>
            new(args.Item1, args.Item2, args.Item3);

        public static bool operator ==(FaceOrientIJK a, FaceOrientIJK b) =>
            a.Face == b.Face && a.Translate == b.Translate && a.CounterClockwiseRotations == b.CounterClockwiseRotations;

        public static bool operator !=(FaceOrientIJK a, FaceOrientIJK b) =>
            a.Face != b.Face || a.Translate != b.Translate || a.CounterClockwiseRotations != b.CounterClockwiseRotations;

        public override bool Equals(object? other) {
            return other is FaceOrientIJK f && Face == f.Face && Translate == f.Translate &&
                CounterClockwiseRotations == f.CounterClockwiseRotations;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Face, Translate, CounterClockwiseRotations);
        }
    }

}
