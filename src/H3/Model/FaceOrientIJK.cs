using System;
using static H3.Constants;

#nullable enable

namespace H3.Model {
    public class FaceOrientIJK {
        public int Face { get; set; }
        public CoordIJK Translate { get; set; } = new CoordIJK();
        public int CounterClockwiseRotations { get; set; }

        #region lookups

        private static readonly CoordIJK Translate1 = new CoordIJK(0, 0, 0);
        private static readonly CoordIJK Translate2 = new CoordIJK(2, 0, 2);
        private static readonly CoordIJK Translate3 = new CoordIJK(2, 2, 0);
        private static readonly CoordIJK Translate4 = new CoordIJK(0, 2, 2);

        public static readonly FaceOrientIJK[,] Neighbours = new FaceOrientIJK[NUM_ICOSA_FACES, 4] {
            {
                // face 0
                new FaceOrientIJK(0, Translate1, 0),  // central face
                new FaceOrientIJK(4, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(1, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(5, Translate4, 3)   // jk quadrant
            },
            {
                // face 1
                new FaceOrientIJK(1, Translate1, 0),  // central face
                new FaceOrientIJK(0, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(2, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(6, Translate4, 3)   // jk quadrant
            },
            {
                // face 2
                new FaceOrientIJK(2, Translate1, 0),  // central face
                new FaceOrientIJK(1, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(3, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(7, Translate4, 3)   // jk quadrant
            },
            {
                // face 3
                new FaceOrientIJK(3, Translate1, 0),  // central face
                new FaceOrientIJK(2, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(4, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(8, Translate4, 3)   // jk quadrant
            },
            {
                // face 4
                new FaceOrientIJK(4, Translate1, 0),  // central face
                new FaceOrientIJK(3, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(0, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(9, Translate4, 3)   // jk quadrant
            },
            {
                // face 5
                new FaceOrientIJK(5, Translate1, 0),   // central face
                new FaceOrientIJK(10, Translate3, 3),  // ij quadrant
                new FaceOrientIJK(14, Translate2, 3),  // ki quadrant
                new FaceOrientIJK(0, Translate4, 3)    // jk quadrant
            },
            {
                // face 6
                new FaceOrientIJK(6, Translate1, 0),   // central face
                new FaceOrientIJK(11, Translate3, 3),  // ij quadrant
                new FaceOrientIJK(10, Translate2, 3),  // ki quadrant
                new FaceOrientIJK(1, Translate4, 3)    // jk quadrant
            },
            {
                // face 7
                new FaceOrientIJK(7, Translate1, 0),   // central face
                new FaceOrientIJK(12, Translate3, 3),  // ij quadrant
                new FaceOrientIJK(11, Translate2, 3),  // ki quadrant
                new FaceOrientIJK(2, Translate4, 3)    // jk quadrant
            },
            {
                // face 8
                new FaceOrientIJK(8, Translate1, 0),   // central face
                new FaceOrientIJK(13, Translate3, 3),  // ij quadrant
                new FaceOrientIJK(12, Translate2, 3),  // ki quadrant
                new FaceOrientIJK(3, Translate4, 3)    // jk quadrant
            },
            {
                // face 9
                new FaceOrientIJK(9, Translate1, 0),   // central face
                new FaceOrientIJK(14, Translate3, 3),  // ij quadrant
                new FaceOrientIJK(13, Translate2, 3),  // ki quadrant
                new FaceOrientIJK(4, Translate4, 3)    // jk quadrant
            },
            {
                // face 10
                new FaceOrientIJK(10, Translate1, 0),  // central face
                new FaceOrientIJK(5, Translate3, 3),   // ij quadrant
                new FaceOrientIJK(6, Translate2, 3),   // ki quadrant
                new FaceOrientIJK(15, Translate4, 3)   // jk quadrant
            },
            {
                // face 11
                new FaceOrientIJK(11, Translate1, 0),  // central face
                new FaceOrientIJK(6, Translate3, 3),   // ij quadrant
                new FaceOrientIJK(7, Translate2, 3),   // ki quadrant
                new FaceOrientIJK(16, Translate4, 3)   // jk quadrant
            },
            {
                // face 12
                new FaceOrientIJK(12, Translate1, 0),  // central face
                new FaceOrientIJK(7, Translate3, 3),   // ij quadrant
                new FaceOrientIJK(8, Translate2, 3),   // ki quadrant
                new FaceOrientIJK(17, Translate4, 3)   // jk quadrant
            },
            {
                // face 13
                new FaceOrientIJK(13, Translate1, 0),  // central face
                new FaceOrientIJK(8, Translate3, 3),   // ij quadrant
                new FaceOrientIJK(9, Translate2, 3),   // ki quadrant
                new FaceOrientIJK(18, Translate4, 3)   // jk quadrant
            },
            {
                // face 14
                new FaceOrientIJK(14, Translate1, 0),  // central face
                new FaceOrientIJK(9, Translate3, 3),   // ij quadrant
                new FaceOrientIJK(5, Translate2, 3),   // ki quadrant
                new FaceOrientIJK(19, Translate4, 3)   // jk quadrant
            },
            {
                // face 15
                new FaceOrientIJK(15, Translate1, 0),  // central face
                new FaceOrientIJK(16, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(19, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(10, Translate4, 3)   // jk quadrant
            },
            {
                // face 16
                new FaceOrientIJK(16, Translate1, 0),  // central face
                new FaceOrientIJK(17, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(15, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(11, Translate4, 3)   // jk quadrant
            },
            {
                // face 17
                new FaceOrientIJK(17, Translate1, 0),  // central face
                new FaceOrientIJK(18, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(16, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(12, Translate4, 3)   // jk quadrant
            },
            {
                // face 18
                new FaceOrientIJK(18, Translate1, 0),  // central face
                new FaceOrientIJK(19, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(17, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(13, Translate4, 3)   // jk quadrant
            },
            {
                // face 19
                new FaceOrientIJK(19, Translate1, 0),  // central face
                new FaceOrientIJK(15, Translate2, 1),  // ij quadrant
                new FaceOrientIJK(18, Translate3, 5),  // ki quadrant
                new FaceOrientIJK(14, Translate4, 3)   // jk quadrant
            }
        };

        #endregion lookups

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
