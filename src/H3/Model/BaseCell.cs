using System;

#nullable enable

namespace H3.Model {

    public class BaseCell {
        public int Cell => Array.IndexOf(LookupTables.BaseCells, this);
        public FaceIJK Home { get; init; } = new();
        public bool IsPentagon { get; init; }
        public int[] ClockwiseOffsetPent { get; init; } = new int[2];
        public bool IsPolarPentagon => Cell == 4 || Cell == 117;

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

        public static implicit operator BaseCell(((int, (int, int, int)), int, (int, int)) tuple) =>
            new() {
                Home = new FaceIJK(tuple.Item1.Item1, tuple.Item1.Item2),
                IsPentagon = tuple.Item2 == 1,
                ClockwiseOffsetPent = new int[2] { tuple.Item3.Item1, tuple.Item3.Item2 }
            };

        public static bool FaceMatchesOffset(int cell, int face) => LookupTables.BaseCells[cell].FaceMatchesOffset(face);

        public static bool operator ==(BaseCell? a, BaseCell? b) {
            return a?.Home == b?.Home && a?.IsPentagon == b?.IsPentagon && a?.ClockwiseOffsetPent[0] == b?.ClockwiseOffsetPent[0] && a?.ClockwiseOffsetPent[1] == b?.ClockwiseOffsetPent[1];
        }

        public static bool operator !=(BaseCell? a, BaseCell? b) {
            return a?.Home != b?.Home || a?.IsPentagon != b?.IsPentagon || a?.ClockwiseOffsetPent[0] != b?.ClockwiseOffsetPent[0] || a?.ClockwiseOffsetPent[1] != b?.ClockwiseOffsetPent[1];
        }

        public override bool Equals(object? other) =>
            other is BaseCell b &&
            Home == b.Home &&
            IsPentagon == b.IsPentagon &&
            ClockwiseOffsetPent[0] == b.ClockwiseOffsetPent[0] &&
            ClockwiseOffsetPent[1] == b.ClockwiseOffsetPent[1];

        public override int GetHashCode() => HashCode.Combine(Home, IsPentagon, ClockwiseOffsetPent);
    }

}
