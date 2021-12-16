using static H3.Data.Constants;

namespace H3.Data {

    public struct BaseCellRotation {
        public int Cell { get; private set; }
        public int CounterClockwiseRotations { get; private set; }

        public const int InvalidRotations = -1;

        public static implicit operator BaseCellRotation((int, int) tuple) =>
            new BaseCellRotation {
                Cell = tuple.Item1,
                CounterClockwiseRotations = tuple.Item2,
            };

        public static int GetRotations(int cell, int face) {
            if (face < 0 || face > NUM_ICOSA_FACES) return InvalidRotations;

            for (var i = 0; i < 3; i+= 1) {
                for (var j = 0; j < 3; j += 1) {
                    for (var k = 0; k < 3; k += 1) {
                        var e = LookupTables.FaceIjkBaseCells[face, i, j, k];
                        if (e.Cell == cell) return e.CounterClockwiseRotations;
                    }
                }
            }

            return InvalidRotations;
        }

    }

}
