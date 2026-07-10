using System;
using static H3.Constants;

#nullable enable

namespace H3.Model; 

public sealed class BaseCellRotation {
    public int Cell { get; private init; }
    public int CounterClockwiseRotations { get; private init; }
    public BaseCell BaseCell { get; private init; } = null!;

    public const int InvalidRotations = -1;

    private BaseCellRotation() { }

    public static implicit operator BaseCellRotation((int, int) tuple) =>
        new() {
            Cell = tuple.Item1,
            CounterClockwiseRotations = tuple.Item2,
            BaseCell =  BaseCells.Cells[tuple.Item1]
        };

    public static int GetCounterClockwiseRotationsForBaseCell(int cell, int face) {
        if (face is < 0 or > NUM_ICOSA_FACES) return InvalidRotations;

        var offset = face * 27;
        for (var i = 0; i < 27; i += 1) {
            if (LookupTables.FaceIjkBaseCellTable[offset + i] == cell) {
                return LookupTables.FaceIjkBaseCellRotationTable[offset + i];
            }
        }

        return InvalidRotations;
    }

    public override bool Equals(object? other) => other is BaseCellRotation r &&
                                                  Cell == r.Cell &&
                                                  CounterClockwiseRotations == r.CounterClockwiseRotations;

    public override int GetHashCode() => HashCode.Combine(Cell, CounterClockwiseRotations);
}