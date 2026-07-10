#nullable enable

using System.Runtime.CompilerServices;

namespace H3.Model;

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
    /// <summary>
    /// Clockwise rotation steps, indexed as [(int)direction * 6 + rotations]
    /// for rotation counts from 0-5.  A single clockwise rotation of a valid
    /// direction digit is multiplication by 3 modulo 7.
    /// </summary>
    private static readonly byte[] Clockwise = {
        0, 0, 0, 0, 0, 0,
        1, 3, 2, 6, 4, 5,
        2, 6, 4, 5, 1, 3,
        3, 2, 6, 4, 5, 1,
        4, 5, 1, 3, 2, 6,
        5, 1, 3, 2, 6, 4,
        6, 4, 5, 1, 3, 2,
        7, 7, 7, 7, 7, 7
    };

    /// <summary>
    /// Counter-clockwise rotation steps, indexed as [(int)direction * 6 + rotations]
    /// for rotation counts from 0-5.  A single counter-clockwise rotation of a
    /// valid direction digit is multiplication by 5 modulo 7.
    /// </summary>
    private static readonly byte[] CounterClockwise = {
        0, 0, 0, 0, 0, 0,
        1, 5, 4, 6, 2, 3,
        2, 3, 1, 5, 4, 6,
        3, 1, 5, 4, 6, 2,
        4, 6, 2, 3, 1, 5,
        5, 4, 6, 2, 3, 1,
        6, 2, 3, 1, 5, 4,
        7, 7, 7, 7, 7, 7
    };

    /// <summary>
    /// Returns the <see cref="Direction"/> that is 60 degrees clockwise to the current
    /// direction.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Direction RotateClockwise(this Direction direction) => direction switch {
        Direction.K => Direction.JK,
        Direction.J => Direction.IJ,
        Direction.JK => Direction.J,
        Direction.I => Direction.IK,
        Direction.IK => Direction.K,
        Direction.IJ => Direction.I,
        _ => direction
    };

    /// <summary>
    /// Returns the <see cref="Direction"/> that is 60 degrees clockwise to the current
    /// direction.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="rotations">number of rotations to perform</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Direction RotateClockwise(this Direction direction, int rotations) {
        return (Direction)Clockwise[(int)direction * 6 + rotations % 6];
    }

    /// <summary>
    /// Returns the <see cref="Direction"/> that is 60 degrees counter-clockwise to the current
    /// direction.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Direction RotateCounterClockwise(this Direction direction) => direction switch {
        Direction.K => Direction.IK,
        Direction.J => Direction.JK,
        Direction.JK => Direction.K,
        Direction.I => Direction.IJ,
        Direction.IK => Direction.I,
        Direction.IJ => Direction.J,
        _ => direction
    };

    /// <summary>
    /// Returns the <see cref="Direction"/> that is 60 degrees counter-clockwise to the current
    /// direction.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="rotations">number of rotations to perform</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Direction RotateCounterClockwise(this Direction direction, int rotations) {
        return (Direction)CounterClockwise[(int)direction * 6 + rotations % 6];
    }
}

public enum Mode {
    Unknown = 0,
    Cell = 1,
    UniEdge = 2,
    Vertex = 4
}

public enum Overage {
    None = 0,
    FaceEdge = 1,
    NewFace = 2
}