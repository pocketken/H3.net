using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable enable

namespace H3.Model; 

public sealed class BaseCell {

    /// <summary>
    /// The cell number, from 0 - 121.
    /// </summary>
    public sbyte Cell { get; internal init; }

    /// <summary>
    /// The home face and IJK address of the cell.
    /// </summary>
    public FaceIJK Home { get; internal init; } = null!;

    /// <summary>
    /// Whether or not this base cell is a pentagon.
    /// </summary>
    public bool IsPentagon { get; internal init; }

    /// <summary>
    /// If a pentagon, the cell's two clockwise offset faces.
    /// </summary>
    internal sbyte[] ClockwiseOffsetPent { get; init; } = null!;

    /// <summary>
    /// Whether or not the cell is a polar pentagon.
    /// </summary>
    public bool IsPolarPentagon { get; internal init; }

    /// <summary>
    /// All of the neighbouring <see cref="BaseCell"/>s of this cell, by
    /// <see cref="Direction"/>.
    /// </summary>
    public sbyte[] NeighbouringCells { get; internal init; } = null!;

    /// <summary>
    /// Indicates the number of counter-clockwise rotations that should
    /// take place to rotate to a given neighbour, by <see cref="Direction"/>.
    /// </summary>
    public sbyte[] NeighbourRotations { get; internal init; } = null!;

    /// <summary>
    /// A map of neighbour cell number to <see cref="Direction"/>.
    /// </summary>
    public Dictionary<sbyte, Direction> NeighbourDirections { get; internal init; } = null!;

    /// <summary>
    /// Whether or not the specified <paramref name="face"/> matches one of this
    /// base cell's offset values.
    /// </summary>
    /// <param name="face"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FaceMatchesOffset(int face) {
        return face == ClockwiseOffsetPent[0] || face == ClockwiseOffsetPent[1];
    }

    /// <summary>
    /// Gets the <see cref="BaseCell"/> that is in the specified
    /// <paramref name="direction"/> from the current base cell.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BaseCell? Neighbour(Direction direction) {
        var cellNumber = NeighbouringCells[(int)direction];
        return cellNumber == LookupTables.INVALID_BASE_CELL ? null : BaseCells.Cells[cellNumber];
    }

    /// <summary>
    /// Gets the <see cref="Direction"/> that is required to move in
    /// order to go from <paramref name="originCell"/> to
    /// <paramref name="destinationCell"/>.
    /// </summary>
    /// <param name="originCell"></param>
    /// <param name="destinationCell"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Direction GetNeighbourDirection(sbyte originCell, sbyte destinationCell) {
        var originBaseCell = BaseCells.Cells[originCell];
        return originBaseCell.NeighbourDirections.TryGetValue(destinationCell, out var direction) ? direction : Direction.Invalid;
    }

    /// <summary>
    /// Whether or not two <see cref="BaseCell"/> instances are equal.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(BaseCell? a, BaseCell? b) {
        if (a is null) return b is not null;
        if (b is null) return true;
        return a.Cell != b.Cell;
    }

    public override bool Equals(object? other) {
        return other is BaseCell b && Cell == b.Cell;
    }

    public override int GetHashCode() => HashCode.Combine(Cell);

}