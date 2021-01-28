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

        public BaseCell Neighbour(CellIndex cellIndex) => LookupTables.BaseCells[LookupTables.Neighbours[Cell, (int)cellIndex]];

        public static CellIndex GetNeighbourCellIndex(int originCell, int neighbouringCell) {
            for (CellIndex idx = CellIndex.Center; idx < CellIndex.Invalid; idx += 1) {
                if (LookupTables.Neighbours[originCell, (int)idx] == neighbouringCell) {
                    return idx;
                }
            }

            return CellIndex.Invalid;
        }

        public static implicit operator BaseCell(((int, (int, int, int)), int, (int, int)) tuple) =>
            new BaseCell {
                Home = new FaceIJK(tuple.Item1.Item1, tuple.Item1.Item2.Item1, tuple.Item1.Item2.Item2, tuple.Item1.Item2.Item3),
                IsPentagon = tuple.Item2 == 1,
                ClockwiseOffsetPent = new int[2] { tuple.Item3.Item1, tuple.Item3.Item2 }
            };

        public static bool FaceMatchesOffset(int cell, int face) => LookupTables.BaseCells[cell].FaceMatchesOffset(face);

        public override bool Equals(object? other) =>
            other is BaseCell b &&
            Home == b.Home &&
            IsPentagon == b.IsPentagon &&
            ClockwiseOffsetPent[0] == b.ClockwiseOffsetPent[0] &&
            ClockwiseOffsetPent[1] == b.ClockwiseOffsetPent[1];

        public override int GetHashCode() => HashCode.Combine(Home, IsPentagon, ClockwiseOffsetPent);
    }

}
