using System;

#nullable enable

namespace H3.Model {

    public class BaseCell {
        public int Cell { get; init; }
        public FaceIJK Home { get; init; } = new();
        public bool IsPentagon { get; init; }
        public int[] ClockwiseOffsetPent { get; init; } = new int[2];
        public bool IsPolarPentagon => Cell is 4 or 117;

        private BaseCell() { }

        public bool FaceMatchesOffset(int face) => ClockwiseOffsetPent[0] == face || ClockwiseOffsetPent[1] == face;

        public BaseCell? Neighbour(Direction direction) {
            var neighbourIndex = LookupTables.Neighbours[Cell, (int)direction];
            if (neighbourIndex == LookupTables.INVALID_BASE_CELL) return null;
            return LookupTables.BaseCells[neighbourIndex];
        }

        public static Direction GetNeighbourDirection(int originCell, int neighbouringCell) {
            for (Direction idx = Direction.Center; idx < Direction.Invalid; idx += 1) {
                if (LookupTables.Neighbours[originCell, (int)idx] == neighbouringCell) {
                    return idx;
                }
            }

            return Direction.Invalid;
        }

        /// <summary>
        /// Creates a <see cref="BaseCell"/> from an input set of parameters.
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns></returns>
        public static implicit operator BaseCell((int, (int, (int, int, int)), int, (int, int)) tuple) =>
            new() {
                Cell = tuple.Item1,
                Home = new FaceIJK(tuple.Item2.Item1, tuple.Item2.Item2),
                IsPentagon = tuple.Item3 == 1,
                ClockwiseOffsetPent = new[] { tuple.Item4.Item1, tuple.Item4.Item2 }
            };

        public static bool FaceMatchesOffset(int cell, int face) => LookupTables.BaseCells[cell].FaceMatchesOffset(face);

        public static bool operator ==(BaseCell? a, BaseCell? b) {
            return a?.Cell == b?.Cell;
        }

        public static bool operator !=(BaseCell? a, BaseCell? b) {
            return a?.Cell != b?.Cell;
        }

        public override bool Equals(object? other) {
            return other is BaseCell b && Cell == b.Cell;
        }

        public override int GetHashCode() => HashCode.Combine(Cell, Home, IsPentagon, ClockwiseOffsetPent);
    }

}
