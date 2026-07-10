using System.Runtime.CompilerServices;

#nullable enable

namespace H3.Model;

public static partial class BaseCells {

    /// <summary>
    /// Whether or not the specified base cell number is one of the 12
    /// pentagon base cells.
    /// </summary>
    /// <param name="cellNumber">base cell number, 0 - 121</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPentagonCellNumber(int cellNumber) =>
        cellNumber < 64
            ? (PentagonMask0 >> cellNumber & 1UL) != 0
            : (PentagonMask1 >> (cellNumber - 64) & 1UL) != 0;

    /// <summary>
    /// Gets the neighbouring base cell number for the specified base cell in
    /// the specified direction; 127 (<see cref="LookupTables.INVALID_BASE_CELL"/>)
    /// for the deleted k-axes direction of a pentagon.
    /// </summary>
    /// <param name="cellNumber">base cell number, 0 - 121</param>
    /// <param name="direction">direction to move in</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static sbyte GetNeighbouringCellNumber(int cellNumber, Direction direction) =>
        NeighbouringCells[cellNumber * 7 + (int)direction];

    /// <summary>
    /// Gets the number of 60 degree ccw rotations required to rotate into
    /// the orientation of the neighbouring base cell of the specified base
    /// cell in the specified direction.
    /// </summary>
    /// <param name="cellNumber">base cell number, 0 - 121</param>
    /// <param name="direction">direction to move in</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static sbyte GetNeighbourCounterClockwiseRotations(int cellNumber, Direction direction) =>
        NeighbourCounterClockwiseRotations[cellNumber * 7 + (int)direction];

}
