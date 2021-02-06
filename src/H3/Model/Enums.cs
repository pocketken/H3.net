#nullable enable

namespace H3.Model {

    public enum Direction {
        Center = 0,
        K = 1,
        J = 2,
        JK = 3,
        I = 4,
        IK = 5,
        IJ = 6,
        Invalid = 7
    }

    public static class DirectionExtensions {
        public static Direction RotateClockwise(this Direction direction) => direction switch {
            Direction.K => Direction.JK,
            Direction.JK => Direction.J,
            Direction.J => Direction.IJ,
            Direction.IJ => Direction.I,
            Direction.I => Direction.IK,
            Direction.IK => Direction.K,
            _ => direction
        };

        public static Direction RotateCounterClockwise(this Direction direction) => direction switch {
            Direction.K => Direction.IK,
            Direction.IK => Direction.I,
            Direction.I => Direction.IJ,
            Direction.IJ => Direction.J,
            Direction.J => Direction.JK,
            Direction.JK => Direction.K,
            _ => direction
        };
    }

    public enum Mode {
        Unknown = 0,
        Hexagon = 1,
        UniEdge = 2
    }

    public enum Overage {
        None = 0,
        FaceEdge = 1,
        NewFace = 2
    }

}
