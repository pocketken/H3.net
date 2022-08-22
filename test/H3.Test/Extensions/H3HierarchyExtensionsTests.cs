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
            Assert.AreEqual(fromCenter[parent.Resolution], parent, "should be equal");
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
        Assert.AreEqual(H3Index.Invalid, actual, "should be H3_NULL");
    }

    [Test]
    public void Test_Upstream_GetParentForResolution_ReturnsSelfAtSameResolution() {
        // Act
        var actual = TestHelpers.SfIndex.GetParentForResolution(TestHelpers.SfIndex.Resolution);

        // Assert
        Assert.AreEqual(TestHelpers.SfIndex, actual, "should return self");
    }

    [Test]
    public void Test_Upstream_GetChildrenForResolution_OneResStep() {
        // Arrange
        var sfHex8 = H3Index.FromLatLng(TestHelpers.SfCoord, 8);

        // Act
        var children = sfHex8.GetChildrenForResolution(9).ToList();

        // Assert
        Assert.AreEqual(ExpectedOneResStepChildren.Length, children.Count, "should return correct child count");
        Assert.IsNotNull(children.Where(ExpectedOneResStepChildren.Contains), "should contain all");
    }

    [Test]
    public void Test_Upstream_GetChildrenForResolution_MultipleResStep() {
        // Arrange
        var sfHex8 = H3Index.FromLatLng(TestHelpers.SfCoord, 8);

        // Act
        var children = sfHex8.GetChildrenForResolution(10).ToList();

        // Assert
        Assert.AreEqual(ExpectedMultipleResStepChildren.Length, children.Count, "should return correct child count");
        Assert.IsNotNull(children.Where(ExpectedMultipleResStepChildren.Contains), "should contain all");
    }

    [Test]
    public void Test_Upstream_GetChildrenForResolution_Pentagon() {
        // Arrange
        var index = new H3Index(0x81083ffffffffff);  // res 1 pentagon

        // Act
        var children = index.GetChildrenForResolution(3).ToList();

        // Assert
        Assert.AreEqual(ExpectedPentagonChildren.Length, children.Count, "should return correct child count");
        Assert.IsNotNull(children.Where(ExpectedPentagonChildren.Contains), "should contain all");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(17)]
    [TestCase(8)]
    public void Test_Upstream_GetChildrenForResolution_FailsOnInvalidResolution(int resolution) {
        // Act
        var actual = TestHelpers.SfIndex.GetChildrenForResolution(resolution);

        // Assert
        Assert.IsEmpty(actual, "should return empty iterator");
    }

    [Test]
    public void Test_Upstream_GetChildrenForResolution_ReturnsSelfAtSomeResolution() {
        // Act
        var actual = TestHelpers.SfIndex.GetChildrenForResolution(TestHelpers.SfIndex.Resolution).ToArray();

        // Assert
        Assert.AreEqual(1, actual.Length, "should return 1 entry");
        Assert.AreEqual(TestHelpers.SfIndex, actual[0], "should return self");
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
        Assert.IsFalse(actual, "should not be a neighbour of itself");
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
            Assert.AreEqual(centers[index], child, "should be equal");
            Assert.AreEqual(index.Resolution + 1, child.Resolution, "should be equal");
            Assert.AreEqual(index, child.GetParentForResolution(index.Resolution), "should be equal");
        }
    }

    [Test]
    public void Test_Upstream_GetChildCenterForResolution_SameResReturnsSelf() {
        // Act
        var actual = TestHelpers.SfIndex.GetChildCenterForResolution(TestHelpers.SfIndex.Resolution);

        // Assert
        Assert.AreEqual(TestHelpers.SfIndex, actual, "should return self for same resolution");
    }

    [Test]
    [TestCase(8)]
    [TestCase(-1)]
    [TestCase(17)]
    public void Test_Upstream_GetChildCenterForResolution_InvalidInputs(int resolution) {
        // Act
        var actual = TestHelpers.SfIndex.GetChildCenterForResolution(resolution);

        // Assert
        Assert.AreEqual(H3Index.Invalid, actual, "should return H3_NULL");
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
        Assert.AreEqual(expectedBaseCell, actual.BaseCellNumber, $"should be {expectedBaseCell}");
        Assert.AreEqual(expectedRotations, rotations, $"{actual.BaseCellNumber} should be {expectedRotations} rotations from {expectedBaseCell}");
    }

    [Test]
    public void Test_Upstream_IsNeighbour_MatchesRing1() {
        // Arrange
        var neighbours = TestHelpers.SfIndex.GetKRing(1)
            .Where(cell => cell.Distance > 0)
            .ToArray();

        // Act
        var actual = neighbours
            .Where(cell => TestHelpers.SfIndex.IsNeighbour(cell.Index))
            .ToArray();

        // Assert
        Assert.AreEqual(neighbours.Length, actual.Length, "should all be neighbours");
    }

    [Test]
    public void Test_Upstream_IsNeighbour_DoesNotMatchRing2() {
        // Arrange
        var neighbours = TestHelpers.SfIndex.GetKRing(2)
            .Where(cell => cell.Distance > 1)
            .ToArray();

        // Act
        var actual = neighbours
            .Where(cell => TestHelpers.SfIndex.IsNeighbour(cell.Index))
            .ToArray();

        // Assert
        Assert.AreEqual(0, actual.Length, "should not be neighbours");
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
        Assert.IsFalse(actual, "invalid indexes should not be neighbours");
    }

    [Test]
    public void Test_Upstream_IsNeighbour_FalseOnResolutionDifference() {
        // Arrange
        H3Index index = new(TestHelpers.SfIndex.GetDirectNeighbour(Direction.IJ).Item1);

        // Act
        var actual = TestHelpers.SfIndex.IsNeighbour(index.GetParentForResolution(7));

        // Assert
        Assert.IsFalse(actual, "should not be neighbours if resolution differs");
    }

    private static void AssertDistinctChildCount(IEnumerable<H3Index> indicies, int expectedCount) {
        var groupCounts = indicies.GroupBy(i => i).Select(g => g.Count()).ToArray();
        Assert.IsEmpty(groupCounts.Where(count => count > 1), "should not contain duplicates");
        Assert.AreEqual(groupCounts.Length, expectedCount, $"should contain {expectedCount} children");
    }
}