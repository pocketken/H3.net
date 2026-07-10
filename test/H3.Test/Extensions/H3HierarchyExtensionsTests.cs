using System;
using System.Collections.Generic;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NUnit.Framework;


namespace H3.Test.Extensions; 

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class H3HierarchyExtensionsTests {
    private static readonly H3Index BaseCell0 = H3Index.Create(0, 0, 0);

    private static readonly H3Index[] ExpectedOneResStepChildren = {
        0x89283080dc3ffff, 0x89283080dc7ffff,
        0x89283080dcbffff, 0x89283080dcfffff,
        0x89283080dd3ffff, 0x89283080dd7ffff,
        0x89283080ddbffff
    };

    private static readonly H3Index[] ExpectedMultipleResStepChildren = {
        0x8a283080dd27fff, 0x8a283080dd37fff, 0x8a283080dc47fff,
        0x8a283080dcdffff, 0x8a283080dc5ffff, 0x8a283080dc27fff,
        0x8a283080ddb7fff, 0x8a283080dc07fff, 0x8a283080dd8ffff,
        0x8a283080dd5ffff, 0x8a283080dc4ffff, 0x8a283080dd47fff,
        0x8a283080dce7fff, 0x8a283080dd1ffff, 0x8a283080dceffff,
        0x8a283080dc6ffff, 0x8a283080dc87fff, 0x8a283080dcaffff,
        0x8a283080dd2ffff, 0x8a283080dcd7fff, 0x8a283080dd9ffff,
        0x8a283080dd6ffff, 0x8a283080dcc7fff, 0x8a283080dca7fff,
        0x8a283080dccffff, 0x8a283080dd77fff, 0x8a283080dc97fff,
        0x8a283080dd4ffff, 0x8a283080dd97fff, 0x8a283080dc37fff,
        0x8a283080dc8ffff, 0x8a283080dcb7fff, 0x8a283080dcf7fff,
        0x8a283080dd87fff, 0x8a283080dda7fff, 0x8a283080dc9ffff,
        0x8a283080dc77fff, 0x8a283080dc67fff, 0x8a283080dc57fff,
        0x8a283080ddaffff, 0x8a283080dd17fff, 0x8a283080dc17fff,
        0x8a283080dd57fff, 0x8a283080dc0ffff, 0x8a283080dd07fff,
        0x8a283080dc1ffff, 0x8a283080dd0ffff, 0x8a283080dc2ffff,
        0x8a283080dd67fff
    };

    private static readonly H3Index[] ExpectedPentagonChildren = {
        0x830800fffffffff, 0x830802fffffffff, 0x830803fffffffff,
        0x830804fffffffff, 0x830805fffffffff, 0x830806fffffffff,
        0x830810fffffffff, 0x830811fffffffff, 0x830812fffffffff,
        0x830813fffffffff, 0x830814fffffffff, 0x830815fffffffff,
        0x830816fffffffff, 0x830818fffffffff, 0x830819fffffffff,
        0x83081afffffffff, 0x83081bfffffffff, 0x83081cfffffffff,
        0x83081dfffffffff, 0x83081efffffffff, 0x830820fffffffff,
        0x830821fffffffff, 0x830822fffffffff, 0x830823fffffffff,
        0x830824fffffffff, 0x830825fffffffff, 0x830826fffffffff,
        0x830828fffffffff, 0x830829fffffffff, 0x83082afffffffff,
        0x83082bfffffffff, 0x83082cfffffffff, 0x83082dfffffffff,
        0x83082efffffffff, 0x830830fffffffff, 0x830831fffffffff,
        0x830832fffffffff, 0x830833fffffffff, 0x830834fffffffff,
        0x830835fffffffff, 0x830836fffffffff
    };

    [Test]
    public void Test_Upstream_GetParentForResolution() {
        // Arrange
        var fromCenter = Enumerable.Range(0, MAX_H3_RES + 1)
            .ToDictionary(res => res, res => H3Index.FromLatLng(TestHelpers.SfCoord, res));
        var sfAt15 = fromCenter[15];

        // Act
        var parents = Enumerable.Range(1, MAX_H3_RES + 1)
            .Select(res => sfAt15.GetParentForResolution(res - 1))
            .ToArray();

        // Assert
        foreach (var parent in parents) {
            Assert.That(parent, Is.EqualTo(fromCenter[parent.Resolution]), "should be equal");
        }
    }

    [Test]
    [TestCase(-1)]
    [TestCase(17)]
    [TestCase(10)]
    public void Test_Upstream_GetParentForResolution_FailsOnInvalidResolution(int resolution) {
        // Act
        var actual = TestHelpers.SfIndex.GetParentForResolution(resolution);

        // Assert
        Assert.That(actual, Is.EqualTo(H3Index.Invalid), "should be H3_NULL");
    }

    [Test]
    public void Test_Upstream_GetParentForResolution_ReturnsSelfAtSameResolution() {
        // Act
        var actual = TestHelpers.SfIndex.GetParentForResolution(TestHelpers.SfIndex.Resolution);

        // Assert
        Assert.That(actual, Is.EqualTo(TestHelpers.SfIndex), "should return self");
    }

    [Test]
    public void Test_Upstream_GetChildrenForResolution_OneResStep() {
        // Arrange
        var sfHex8 = H3Index.FromLatLng(TestHelpers.SfCoord, 8);

        // Act
        var children = sfHex8.GetChildrenForResolution(9).ToList();

        // Assert
        Assert.That(children.Count, Is.EqualTo(ExpectedOneResStepChildren.Length), "should return correct child count");
        Assert.That(children.Where(ExpectedOneResStepChildren.Contains), Is.Not.Null, "should contain all");
    }

    [Test]
    public void Test_Upstream_GetChildrenForResolution_MultipleResStep() {
        // Arrange
        var sfHex8 = H3Index.FromLatLng(TestHelpers.SfCoord, 8);

        // Act
        var children = sfHex8.GetChildrenForResolution(10).ToList();

        // Assert
        Assert.That(children.Count, Is.EqualTo(ExpectedMultipleResStepChildren.Length), "should return correct child count");
        Assert.That(children.Where(ExpectedMultipleResStepChildren.Contains), Is.Not.Null, "should contain all");
    }

    [Test]
    public void Test_Upstream_GetChildrenForResolution_Pentagon() {
        // Arrange
        var index = new H3Index(0x81083ffffffffff);  // res 1 pentagon

        // Act
        var children = index.GetChildrenForResolution(3).ToList();

        // Assert
        Assert.That(children.Count, Is.EqualTo(ExpectedPentagonChildren.Length), "should return correct child count");
        Assert.That(children.Where(ExpectedPentagonChildren.Contains), Is.Not.Null, "should contain all");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(17)]
    [TestCase(8)]
    public void Test_Upstream_GetChildrenForResolution_FailsOnInvalidResolution(int resolution) {
        // Act
        var actual = TestHelpers.SfIndex.GetChildrenForResolution(resolution);

        // Assert
        Assert.That(actual, Is.Empty, "should return empty iterator");
    }

    [Test]
    public void Test_Upstream_GetChildrenForResolution_ReturnsSelfAtSomeResolution() {
        // Act
        var actual = TestHelpers.SfIndex.GetChildrenForResolution(TestHelpers.SfIndex.Resolution).ToArray();

        // Assert
        Assert.That(actual.Length, Is.EqualTo(1), "should return 1 entry");
        Assert.That(actual[0], Is.EqualTo(TestHelpers.SfIndex), "should return self");
    }

    [Test]
    public void Test_GetChildrenForResolution_TestIndexValue() {
        // Arrange
        H3Index h3 = new(TestHelpers.TestIndexValue);

        // Act
        var children = h3.GetChildrenForResolution(15).ToArray();

        // Assert
        TestHelpers.AssertAll(TestHelpers.TestIndexChildrenAtRes15, children);
    }

    [Test]
    public void Test_Upstream_IsNeighbour_NotANeighbourOfThyself() {
        // Act
        var actual = TestHelpers.SfIndex.IsNeighbour(TestHelpers.SfIndex);

        // Assert
        Assert.That(actual, Is.False, "should not be a neighbour of itself");
    }

    [Test]
    public void Test_Upstream_GetChildCenterForResolution() {
        // Arrange
        var center = H3Index.Create(8, 4, Direction.J).ToLatLng();
        var indexes = Enumerable.Range(0, MAX_H3_RES)
            .Select(res => H3Index.FromLatLng(center, res));
        var centers = indexes.ToDictionary(i => i, i => H3Index.FromLatLng(i.ToLatLng(), i.Resolution + 1));

        // Act
        var children = indexes.ToDictionary(i => i, i => i.GetChildCenterForResolution(i.Resolution + 1));

        // Assert
        foreach (var index in indexes) {
            var child = children[index];
            Assert.That(child, Is.EqualTo(centers[index]), "should be equal");
            Assert.That(child.Resolution, Is.EqualTo(index.Resolution + 1), "should be equal");
            Assert.That(child.GetParentForResolution(index.Resolution), Is.EqualTo(index), "should be equal");
        }
    }

    [Test]
    public void Test_Upstream_GetChildCenterForResolution_SameResReturnsSelf() {
        // Act
        var actual = TestHelpers.SfIndex.GetChildCenterForResolution(TestHelpers.SfIndex.Resolution);

        // Assert
        Assert.That(actual, Is.EqualTo(TestHelpers.SfIndex), "should return self for same resolution");
    }

    [Test]
    [TestCase(8)]
    [TestCase(-1)]
    [TestCase(17)]
    public void Test_Upstream_GetChildCenterForResolution_InvalidInputs(int resolution) {
        // Act
        var actual = TestHelpers.SfIndex.GetChildCenterForResolution(resolution);

        // Assert
        Assert.That(actual, Is.EqualTo(H3Index.Invalid), "should return H3_NULL");
    }

    [Test]
    [TestCase(Direction.Center, 0, 0)]
    [TestCase(Direction.K, 1, 5)]
    [TestCase(Direction.J, 5, 0)]
    [TestCase(Direction.JK, 2, 0)]
    [TestCase(Direction.I, 4, 1)]
    [TestCase(Direction.IK, 3, 5)]
    [TestCase(Direction.IJ, 8, 1)]
    public void Test_GetDirectNeighbour_BaseCells(Direction direction, int expectedBaseCell, int baseRotations) {
        // Arrange
        var expectedRotations = BaseCells.Cells[expectedBaseCell].IsPentagon ? baseRotations + 1 : baseRotations;

        // Act
        var (actual, rotations) = BaseCell0.GetDirectNeighbour(direction);

        // Assert
        Assert.That(actual.BaseCellNumber, Is.EqualTo(expectedBaseCell), $"should be {expectedBaseCell}");
        Assert.That(rotations, Is.EqualTo(expectedRotations), $"{actual.BaseCellNumber} should be {expectedRotations} rotations from {expectedBaseCell}");
    }

    [Test]
    public void Test_Upstream_IsNeighbour_MatchesRing1() {
        // Arrange
        var neighbours = TestHelpers.SfIndex.GridDiskDistances(1)
            .Where(cell => cell.Distance > 0)
            .ToArray();

        // Act
        var actual = neighbours
            .Where(cell => TestHelpers.SfIndex.IsNeighbour(cell.Index))
            .ToArray();

        // Assert
        Assert.That(actual.Length, Is.EqualTo(neighbours.Length), "should all be neighbours");
    }

    [Test]
    public void Test_Upstream_IsNeighbour_DoesNotMatchRing2() {
        // Arrange
        var neighbours = TestHelpers.SfIndex.GridDiskDistances(2)
            .Where(cell => cell.Distance > 1)
            .ToArray();

        // Act
        var actual = neighbours
            .Where(cell => TestHelpers.SfIndex.IsNeighbour(cell.Index))
            .ToArray();

        // Assert
        Assert.That(actual.Length, Is.EqualTo(0), "should not be neighbours");
    }

    [Test]
    public void Test_Upstream_IsNeighbour_FalseOnInvalid() {
        // Arrange
        H3Index index = new(TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1) {
            Mode = Mode.UniEdge
        };

        // Act
        var actual = TestHelpers.SfIndex.IsNeighbour(index);

        // Assert
        Assert.That(actual, Is.False, "invalid indexes should not be neighbours");
    }

    [Test]
    public void Test_Upstream_IsNeighbour_FalseOnResolutionDifference() {
        // Arrange
        H3Index index = new(TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1);

        // Act
        var actual = TestHelpers.SfIndex.IsNeighbour(index.GetParentForResolution(7));

        // Assert
        Assert.That(actual, Is.False, "should not be neighbours if resolution differs");
    }

    public static IEnumerable<TestCaseData> ChildPosParentTestCases {
        get {
            var res0 = H3Index.GetRes0Cells();
            var pentagonRes1 = H3Index.GetPentagons(0)
                .SelectMany(pentagon => pentagon.GetChildrenForResolution(1));

            return res0.Concat(pentagonRes1)
                .SelectMany(parent => Enumerable.Range(parent.Resolution, 3)
                    .Select(childResolution => new TestCaseData(parent, childResolution)
                        .SetArgDisplayNames($"{parent}", $"{childResolution}")));
        }
    }

    [Test]
    [TestCaseSource(typeof(H3HierarchyExtensionsTests), nameof(ChildPosParentTestCases))]
    public void Test_Upstream_CellToChildPos_MatchesChildOrder(H3Index parent, int childResolution) {
        // Arrange
        var children = parent.GetChildrenForResolution(childResolution);

        // Act
        var positions = children.Select(child => child.CellToChildPos(parent.Resolution));

        // Assert
        var expected = 0L;
        foreach (var position in positions) {
            Assert.That(position, Is.EqualTo(expected), $"should be at position {expected} within {parent}");
            expected += 1;
        }
    }

    [Test]
    [TestCaseSource(typeof(H3HierarchyExtensionsTests), nameof(ChildPosParentTestCases))]
    public void Test_Upstream_ChildPosToCell_MatchesChildOrder(H3Index parent, int childResolution) {
        // Arrange
        var children = parent.GetChildrenForResolution(childResolution).ToList();

        // Act
        var cells = Enumerable.Range(0, children.Count)
            .Select(position => parent.ChildPosToCell(position, childResolution));

        // Assert
        Assert.That(cells, Is.EqualTo(children), "should produce children in child order");
    }

    [Test]
    [TestCaseSource(typeof(H3HierarchyExtensionsTests), nameof(ChildPosParentTestCases))]
    public void Test_Upstream_CellToChildrenSize_MatchesChildCount(H3Index parent, int childResolution) {
        // Arrange
        var expected = parent.GetChildrenForResolution(childResolution).LongCount();

        // Act
        var actual = parent.CellToChildrenSize(childResolution);

        // Assert
        Assert.That(actual, Is.EqualTo(expected), "should match enumerated child count");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(42)]
    [TestCase(9)]
    public void Test_Upstream_CellToChildPos_InvalidResolutions(int parentResolution) {
        // Arrange
        H3Index child = 0x88283080ddfffff;

        // Act
        Action actual = () => child.CellToChildPos(parentResolution);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(actual, "should throw for resolution not between 0 and the child's resolution");
    }

    [Test]
    [TestCase(42)]
    [TestCase(-1)]
    [TestCase(7)]
    public void Test_Upstream_ChildPosToCell_InvalidResolutions(int childResolution) {
        // Arrange
        H3Index parent = 0x88283080ddfffff;

        // Act
        Action actual = () => parent.ChildPosToCell(27, childResolution);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(actual, "should throw for resolution not between the parent's resolution and 15");
    }

    [Test]
    [TestCase(-1L)]
    [TestCase(49L)]
    public void Test_Upstream_ChildPosToCell_InvalidPositions(long position) {
        // Arrange
        H3Index parent = 0x88283080ddfffff;

        // Act
        Action actual = () => parent.ChildPosToCell(position, 10);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(actual, "should throw for position outside of the child count");
    }

    [Test]
    public void Test_Upstream_ChildPosToCell_MaximumValidPosition() {
        // Arrange
        H3Index parent = 0x88283080ddfffff;

        // Act
        Action actual = () => parent.ChildPosToCell(48, 10);

        // Assert
        Assert.DoesNotThrow(actual, "should not throw for maximum valid position");
    }

    [Test]
    public void Test_Upstream_CellToChildPos_InvalidDigit() {
        // Arrange
        var child = new H3Index(0x88283080ddfffff);
        child.SetDirectionForResolution(6, Direction.Invalid);

        // Act
        Action actual = () => child.CellToChildPos(0);

        // Assert
        Assert.Throws<ArgumentException>(actual, "should throw for invalid digit");
    }

    [Test]
    public void Test_Upstream_CellToChildPos_InvalidDigitUnderPentagon() {
        // Arrange
        var child = new H3Index(0x870800006ffffff);
        child.SetDirectionForResolution(7, Direction.Invalid);

        // Act
        Action actual = () => child.CellToChildPos(0);

        // Assert
        Assert.Throws<ArgumentException>(actual, "should throw for invalid digit under pentagon");
    }

    [Test]
    public void Test_Upstream_CellToChildPos_DeletedKSubsequence() {
        // Arrange
        var child = H3Index.Create(8, 4, Direction.K);

        // Act
        Action actual = () => child.CellToChildPos(0);

        // Assert
        Assert.Throws<ArgumentException>(actual, "should throw for deleted k subsequence");
    }

    [Test]
    [TestCase(false, 0, ExpectedResult = 1L)]
    [TestCase(false, 1, ExpectedResult = 7L)]
    [TestCase(false, 2, ExpectedResult = 49L)]
    [TestCase(true, 0, ExpectedResult = 1L)]
    [TestCase(true, 1, ExpectedResult = 6L)]
    [TestCase(true, 2, ExpectedResult = 41L)]
    public long Test_Upstream_CellToChildrenSize(bool pentagon, int resolutionOffset) {
        var origin = pentagon ? H3Index.Create(4, 4, 0) : TestHelpers.SfIndex;
        return origin.CellToChildrenSize(origin.Resolution + resolutionOffset);
    }

    [Test]
    public void Test_Upstream_CellToChildrenSize_InvalidResolution() {
        // Arrange
        var origin = TestHelpers.SfIndex;

        // Act
        Action actual = () => origin.CellToChildrenSize(origin.Resolution - 1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(actual, "should throw for coarser resolution");
    }

    private static void AssertDistinctChildCount(IEnumerable<H3Index> indicies, int expectedCount) {
        var groupCounts = indicies.GroupBy(i => i).Select(g => g.Count()).ToArray();
        Assert.That(groupCounts.Where(count => count > 1), Is.Empty, "should not contain duplicates");
        Assert.That(expectedCount, Is.EqualTo(groupCounts.Length), $"should contain {expectedCount} children");
    }
}