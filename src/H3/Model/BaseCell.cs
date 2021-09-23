using System;

#nullable enable

namespace H3.Model {

    /// <summary>
    /// Definition for one of the 122 base cells that form the H3 indexing scheme.
    /// </summary>
    public sealed class BaseCell {
        /// <summary>
        /// The cell number, from 0 - 121.
        /// </summary>
        public int Cell { get; private init; }

        /// <summary>
        /// The home face and IJK address of the cell.
        /// </summary>
        public FaceIJK Home { get; private init; } = null!;

        /// <summary>
        /// Whether or not this base cell is a pentagon.
        /// </summary>
        public bool IsPentagon { get; private init; }

        /// <summary>
        /// If a pentagon, the cell's two clockwise offset faces.
        /// </summary>
        public int[] ClockwiseOffsetPent { get; private init; } = null!;

        /// <summary>
        /// Whether or not the cell is a polar pentagon.
        /// </summary>
        public bool IsPolarPentagon => Cell is 4 or 117;

        private BaseCell() { }

        /// <summary>
        /// Whether or not the specified <paramref name="face"/> matches one of this
        /// base cell's <see cref="ClockwiseOffsetPent"/> values.
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public bool FaceMatchesOffset(int face) => ClockwiseOffsetPent[0] == face || ClockwiseOffsetPent[1] == face;

        /// <summary>
        /// Returns the neighbouring <see cref="BaseCell"/> in the specified <see cref="Direction"/>.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public BaseCell? Neighbour(Direction direction) {
            var neighbourIndex = LookupTables.Neighbours[Cell, (int)direction];
            return neighbourIndex == LookupTables.INVALID_BASE_CELL ? null : LookupTables.BaseCells[neighbourIndex];
        }

        /// <summary>
        /// Gets the <see cref="Direction"/> required to move between the two specified <see cref="BaseCell"/>
        /// numbers.  Returns <see cref="Direction.Invalid"/> if the cells are not neighbours.
        /// </summary>
        /// <param name="originCell"></param>
        /// <param name="neighbouringCell"></param>
        /// <returns></returns>
        public static Direction GetNeighbourDirection(int originCell, int neighbouringCell) {
            for (var idx = Direction.Center; idx < Direction.Invalid; idx += 1) {
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

        /// <summary>
        /// Whether or not two <see cref="BaseCell"/> instances are equal.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(BaseCell? a, BaseCell? b) {
            if (a is null) return b is null;
            if (b is null) return false;
            return a.Cell == b.Cell;
        }

        /// <summary>
        /// Whether or not two <see cref="BaseCell"/> instances are not equal.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(BaseCell? a, BaseCell? b) {
            if (a is null) return b is not null;
            if (b is null) return true;
            return a.Cell != b.Cell;
        }

        public override bool Equals(object? other) {
            return other is BaseCell b && Cell == b.Cell;
        }

        public override int GetHashCode() => HashCode.Combine(Cell, Home, IsPentagon, ClockwiseOffsetPent);
    }

}
