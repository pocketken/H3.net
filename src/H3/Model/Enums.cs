#nullable enable

namespace H3.Model {

    public enum CellIndex {
        Center = 0,
        K = 1,
        J = 2,
        JK = 3,
        I = 4,
        IK = 5,
        IJ = 6,
        Invalid = 7
    }

    public static class CellIndexExtensions {
        public static CellIndex Rotate60Clockwise(this CellIndex cell) => cell switch {
            CellIndex.K => CellIndex.JK,
            CellIndex.JK => CellIndex.J,
            CellIndex.J => CellIndex.IJ,
            CellIndex.IJ => CellIndex.I,
            CellIndex.I => CellIndex.IK,
            CellIndex.IK => CellIndex.K,
            _ => cell
        };

        public static CellIndex Rotate60CounterClockwise(this CellIndex cell) => cell switch {
            CellIndex.K => CellIndex.IK,
            CellIndex.IK => CellIndex.I,
            CellIndex.I => CellIndex.IJ,
            CellIndex.IJ => CellIndex.J,
            CellIndex.J => CellIndex.JK,
            CellIndex.JK => CellIndex.K,
            _ => cell
        };
    }

    public enum Mode {
        Unknown = 0,
        Hexagon = 1,
        UniEdge = 2
    }
}
