using System.Collections.Generic;
using System.Linq;
using H3.Model;
using NUnit.Framework;

using static H3.Constants;

namespace H3.Test.Model;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LookupTablesTests {

    private static IEnumerable<int> AllBaseCellNumbers => Enumerable.Range(0, NUM_BASE_CELLS);

    private static IEnumerable<int> AllResolutions => Enumerable.Range(0, MAX_H3_RES + 1);

    private static IEnumerable<Direction> AllDirections =>
        Enumerable.Range(0, 7).Select(d => (Direction)d);

    [Test]
    [TestCaseSource(nameof(AllBaseCellNumbers))]
    public void Test_BaseCells_IsPentagonCellNumber_MatchesCellFlag(int cellNumber) {
        // Act
        var actual = BaseCells.IsPentagonCellNumber(cellNumber);

        // Assert
        Assert.That(actual, Is.EqualTo(BaseCells.Cells[cellNumber].IsPentagon), $"pentagon flag mismatch for base cell {cellNumber}");
    }

    [Test]
    [TestCaseSource(nameof(AllBaseCellNumbers))]
    public void Test_BaseCells_GetNeighbourDirection_InvertsNeighbourLookup(int cellNumber) {
        // Act
        var roundTripped = AllDirections
            .Select(direction => (
                Direction: direction,
                Neighbour: BaseCells.GetNeighbouringCellNumber(cellNumber, direction)))
            .Where(pair => pair.Neighbour != LookupTables.INVALID_BASE_CELL)
            .Select(pair => (
                Expected: pair.Direction,
                Actual: BaseCell.GetNeighbourDirection((sbyte)cellNumber, pair.Neighbour)));

        // Assert
        foreach (var (expected, actual) in roundTripped) {
            Assert.That(actual, Is.EqualTo(expected), $"direction mismatch for base cell {cellNumber}");
        }
    }

    [Test]
    [TestCaseSource(nameof(AllBaseCellNumbers))]
    public void Test_LookupTables_BaseCellIsCanonicallyOrientedOnHomeFace(int cellNumber) {
        // Act
        var rotations = BaseCellRotation.GetCounterClockwiseRotationsForBaseCell(
            cellNumber, BaseCells.Cells[cellNumber].Home.Face);

        // Assert
        Assert.That(rotations, Is.EqualTo(0), $"base cell {cellNumber} should not be rotated on its home face");
    }

    [Test]
    public void Test_LookupTables_FaceIjkBaseCells_MatchesFlatTables() {
        // Act
        var lookup = LookupTables.FaceIjkBaseCells;

        // Assert
        for (var face = 0; face < NUM_ICOSA_FACES; face += 1) {
            for (var i = 0; i < 3; i += 1) {
                for (var j = 0; j < 3; j += 1) {
                    for (var k = 0; k < 3; k += 1) {
                        var flat = LookupTables.FlatFaceIjkIndex(face, i, j, k);
                        Assert.That(lookup[face, i, j, k].Cell, Is.EqualTo(LookupTables.FaceIjkBaseCellTable[flat]), $"cell mismatch at {face},{i},{j},{k}");
                        Assert.That(lookup[face, i, j, k].CounterClockwiseRotations, Is.EqualTo(LookupTables.FaceIjkBaseCellRotationTable[flat]), $"rotation mismatch at {face},{i},{j},{k}");
                    }
                }
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(AllResolutions))]
    public void Test_LookupTables_PentagonIndexesPerResolution_MatchesCreate(int resolution) {
        // Act
        var expected = AllBaseCellNumbers
            .Where(BaseCells.IsPentagonCellNumber)
            .Select(cellNumber => H3Index.Create(resolution, cellNumber, Direction.Center));

        // Assert
        Assert.That(LookupTables.PentagonIndexesPerResolution[resolution], Is.EqualTo(expected), $"pentagon indexes mismatch at resolution {resolution}");
    }

    [Test]
    public void Test_UnitVectors_MatchDirectionBits() {
        // Act
        var actual = AllDirections.Take(7)
            .Select(direction => (
                Direction: direction,
                Expected: new CoordIJK(((int)direction >> 2) & 1, ((int)direction >> 1) & 1, (int)direction & 1)));

        // Assert
        foreach (var (direction, expected) in actual) {
            Assert.That(LookupTables.UnitVectors[(int)direction], Is.EqualTo(expected), $"unit vector mismatch for {direction}");
        }
    }

    [Test]
    [TestCaseSource(nameof(AllDirections))]
    public void Test_DirectionExtensions_MultiRotationMatchesRepeatedSingleRotation(Direction direction) {
        // Act
        var rotated = Enumerable.Range(0, 12)
            .Select(rotations => (
                Rotations: rotations,
                Clockwise: direction.RotateClockwise(rotations),
                CounterClockwise: direction.RotateCounterClockwise(rotations)));

        // Assert
        foreach (var (rotations, clockwise, counterClockwise) in rotated) {
            var expectedClockwise = direction;
            var expectedCounterClockwise = direction;
            for (var i = 0; i < rotations % 6; i += 1) {
                expectedClockwise = expectedClockwise.RotateClockwise();
                expectedCounterClockwise = expectedCounterClockwise.RotateCounterClockwise();
            }

            Assert.That(clockwise, Is.EqualTo(expectedClockwise), $"{direction} cw x{rotations}");
            Assert.That(counterClockwise, Is.EqualTo(expectedCounterClockwise), $"{direction} ccw x{rotations}");
        }
    }

    [Test]
    [TestCaseSource(nameof(AllDirections))]
    public void Test_DirectionExtensions_SingleRotationsAreInverse(Direction direction) {
        // Act
        var roundTripped = direction.RotateClockwise().RotateCounterClockwise();

        // Assert
        Assert.That(roundTripped, Is.EqualTo(direction), $"{direction} should round trip");
    }

    [Test]
    [TestCaseSource(nameof(AllDirections))]
    public void Test_CoordIJK_ToNeighbour_MatchesUnitVectorTranslation(Direction direction) {
        // Arrange
        CoordIJK coord = new(4, 1, 2);

        // Act
        var actual = CoordIJK.ToNeighbour(coord, direction);

        // Assert
        var expected = direction is > Direction.Center and < Direction.Invalid
            ? CoordIJK.Normalize(coord + LookupTables.UnitVectors[(int)direction])
            : coord;
        Assert.That(actual, Is.EqualTo(expected), $"neighbour mismatch for {direction}");
    }

}
