namespace H3.Data {

    /// <summary>
    /// Definition for one of the 122 base cells that form the H3 indexing scheme.
    /// </summary>
    public sealed class BaseCellData {
        /// <summary>
        /// The cell number, from 0 - 121.
        /// </summary>
        public int Cell { get; private set; }

        /// <summary>
        /// The home face and IJK address of the cell.
        /// </summary>
        public BaseCellHomeAddress Home { get; private set; }

        /// <summary>
        /// Whether or not this base cell is a pentagon.
        /// </summary>
        public bool IsPentagon { get; private set; }

        /// <summary>
        /// If a pentagon, the cell's two clockwise offset faces.
        /// </summary>
        public int[] ClockwiseOffsetPent { get; private set; }

        private BaseCellData() { }

        /// <summary>
        /// Creates a <see cref="BaseCell"/> from an input set of parameters.
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns></returns>
        public static implicit operator BaseCellData((int, (int, (int, int, int)), int, (int, int)) tuple) =>
            new BaseCellData {
                Cell = tuple.Item1,
                Home = new BaseCellHomeAddress {
                    Face = tuple.Item2.Item1,
                    I = tuple.Item2.Item2.Item1,
                    J = tuple.Item2.Item2.Item2,
                    K = tuple.Item2.Item2.Item3
                },
                IsPentagon = tuple.Item3 == 1,
                ClockwiseOffsetPent = new[] { tuple.Item4.Item1, tuple.Item4.Item2 }
            };

    }

}
