using System;
using System.Collections.Generic;
using System.Linq;
using H3.Algorithms;
using H3.Extensions;
using H3.Model;
using NUnit.Framework;
using static H3.Test.TestHelpers;

namespace H3.Test;

/// <summary>
/// Correctness tests for the additive zero-allocation span / buffer-fill
/// overloads.  The governing contract is <b>parity</b>: each fill method must
/// produce exactly the same cells, in the same order, and the same count as its
/// streaming <see cref="IEnumerable{T}"/> sibling — for hexagons, pentagons and
/// the degenerate (k == 0, same-resolution, empty) cases.  Allocation behaviour
/// is measured by the benchmarks, not asserted here.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SpanApiTests {

    private static H3Index Pentagon => H3Index.GetPentagons(9).First();

    // ------------------------------------------------------------------
    // GridDiskDistances / GridDisk / MaxGridDiskSize
    // ------------------------------------------------------------------

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(5)]
    public void Test_GridDiskDistances_Span_MatchesStreaming_Hexagon(int k) {
        // Arrange
        var expected = SfIndex.GridDiskDistances(k).ToArray();
        var buffer = new RingCell[Rings.MaxGridDiskSize(k)];

        // Act
        var count = SfIndex.GridDiskDistances(k, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(expected.Length), "count should match the streaming result");
        for (var i = 0; i < count; i += 1) {
            Assert.That(buffer[i].Index, Is.EqualTo(expected[i].Index), $"cell {i}");
            Assert.That(buffer[i].Distance, Is.EqualTo(expected[i].Distance), $"distance {i}");
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Test_GridDiskDistances_Span_MatchesStreaming_Pentagon(int k) {
        // Arrange — a pentagon origin forces the pentagon-safe BFS fallback in
        // both the streaming and the span implementations
        var origin = Pentagon;
        var expected = origin.GridDiskDistances(k).ToArray();
        var buffer = new RingCell[Rings.MaxGridDiskSize(k)];

        // Act
        var count = origin.GridDiskDistances(k, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(expected.Length), "count should match the streaming result");
        for (var i = 0; i < count; i += 1) {
            Assert.That(buffer[i].Index, Is.EqualTo(expected[i].Index), $"cell {i}");
            Assert.That(buffer[i].Distance, Is.EqualTo(expected[i].Distance), $"distance {i}");
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    public void Test_GridDisk_Span_MatchesStreamingCells(int k) {
        // Arrange
        var expected = SfIndex.GridDiskDistances(k).Select(c => c.Index).ToArray();
        var buffer = new H3Index[Rings.MaxGridDiskSize(k)];

        // Act
        var count = SfIndex.GridDisk(k, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(expected.Length));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [TestCase(0, 1)]
    [TestCase(1, 7)]
    [TestCase(2, 19)]
    [TestCase(3, 37)]
    public void Test_MaxGridDiskSize_KnownValues(int k, int expected) {
        // Act
        var actual = Rings.MaxGridDiskSize(k);

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Test_GridDiskDistances_Span_TooSmall_Throws() {
        // Arrange
        var buffer = new RingCell[Rings.MaxGridDiskSize(2) - 1];

        // Act / Assert
        Assert.That(() => SfIndex.GridDiskDistances(2, buffer), Throws.ArgumentException);
    }

    [Test]
    public void Test_MaxGridDiskSize_Negative_Throws() {
        // Act / Assert
        Assert.That(() => Rings.MaxGridDiskSize(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    // ------------------------------------------------------------------
    // GridRingUnsafe / MaxGridRingSize
    // ------------------------------------------------------------------

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Test_GridRingUnsafe_Span_MatchesStreaming_Hexagon(int k) {
        // Arrange
        var expected = SfIndex.GridRingUnsafe(k).ToArray();
        var buffer = new H3Index[Rings.MaxGridRingSize(k)];

        // Act
        var count = SfIndex.GridRingUnsafe(k, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(expected.Length));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Test_GridRingUnsafe_Span_Pentagon_Throws() {
        // Arrange — the streaming overload throws for a pentagon origin; the span
        // overload must behave identically
        var origin = Pentagon;
        var buffer = new H3Index[Rings.MaxGridRingSize(1)];

        // Act / Assert
        Assert.That(() => origin.GridRingUnsafe(1, buffer), Throws.InstanceOf<HexRingException>());
    }

    [TestCase(0, 1)]
    [TestCase(1, 6)]
    [TestCase(2, 12)]
    [TestCase(3, 18)]
    public void Test_MaxGridRingSize_KnownValues(int k, int expected) {
        // Act
        var actual = Rings.MaxGridRingSize(k);

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Test_GridRingUnsafe_Span_TooSmall_Throws() {
        // Arrange
        var buffer = new H3Index[Rings.MaxGridRingSize(2) - 1];

        // Act / Assert
        Assert.That(() => SfIndex.GridRingUnsafe(2, buffer), Throws.ArgumentException);
    }

    // ------------------------------------------------------------------
    // GetChildrenForResolution / CellToChildrenSize
    // ------------------------------------------------------------------

    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    public void Test_GetChildren_Span_MatchesStreaming_Hexagon(int childResolution) {
        // Arrange
        var expected = SfIndex.GetChildrenForResolution(childResolution).ToArray();
        var size = SfIndex.CellToChildrenSize(childResolution);
        var buffer = new H3Index[size];

        // Act
        var count = SfIndex.GetChildrenForResolution(childResolution, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(size), "count should equal CellToChildrenSize");
        Assert.That(count, Is.EqualTo(expected.Length));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [TestCase(10)]
    [TestCase(11)]
    public void Test_GetChildren_Span_MatchesStreaming_Pentagon(int childResolution) {
        // Arrange
        var origin = Pentagon;
        var expected = origin.GetChildrenForResolution(childResolution).ToArray();
        var size = origin.CellToChildrenSize(childResolution);
        var buffer = new H3Index[size];

        // Act
        var count = origin.GetChildrenForResolution(childResolution, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(size));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Test_GetChildren_Span_SameResolution_ReturnsSelf() {
        // Arrange
        var buffer = new H3Index[1];

        // Act
        var count = SfIndex.GetChildrenForResolution(9, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(1));
        Assert.That(buffer[0], Is.EqualTo(SfIndex));
    }

    [Test]
    public void Test_GetChildren_Span_CoarserResolution_ReturnsZero() {
        // Arrange — a child resolution coarser than the cell is invalid; the
        // streaming overload yields nothing, so the span overload returns 0
        var buffer = new H3Index[8];

        // Act
        var count = SfIndex.GetChildrenForResolution(8, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void Test_GetChildren_Span_TooSmall_Throws() {
        // Arrange
        var size = (int)SfIndex.CellToChildrenSize(11);
        var buffer = new H3Index[size - 1];

        // Act / Assert
        Assert.That(() => SfIndex.GetChildrenForResolution(11, buffer), Throws.ArgumentException);
    }

    // ------------------------------------------------------------------
    // GridPathCells / GridPathCellsSize
    // ------------------------------------------------------------------

    [Test]
    public void Test_GridPathCells_Span_MatchesStreaming() {
        // Arrange — every cell within k=3 of SF exercises a range of path lengths
        var origin = SfIndex;
        foreach (var target in origin.GridDiskDistances(3).Select(c => c.Index)) {
            var expected = origin.GridPathCells(target).ToArray();
            var size = origin.GridPathCellsSize(target);
            var buffer = new H3Index[size];

            // Act
            var count = origin.GridPathCells(target, buffer);

            // Assert
            Assert.That(size, Is.EqualTo(expected.Length), $"size for {target}");
            Assert.That(count, Is.EqualTo(expected.Length), $"count for {target}");
            Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection, $"path for {target}");
        }
    }

    [Test]
    public void Test_GridPathCells_Span_SameCell_ReturnsSelf() {
        // Arrange
        var buffer = new H3Index[1];

        // Act
        var count = SfIndex.GridPathCells(SfIndex, buffer);

        // Assert
        Assert.That(SfIndex.GridPathCellsSize(SfIndex), Is.EqualTo(1));
        Assert.That(count, Is.EqualTo(1));
        Assert.That(buffer[0], Is.EqualTo(SfIndex));
    }

    [Test]
    public void Test_GridPathCells_Span_TooSmall_Throws() {
        // Arrange
        var target = SfIndex.GridDiskDistances(3).Select(c => c.Index).Last();
        var size = SfIndex.GridPathCellsSize(target);
        var buffer = new H3Index[size - 1];

        // Act / Assert
        Assert.That(() => SfIndex.GridPathCells(target, buffer), Throws.ArgumentException);
    }

    // ------------------------------------------------------------------
    // GetCellBoundaryVertices / MaxCellBoundaryVertices
    // ------------------------------------------------------------------

    [Test]
    public void Test_GetCellBoundaryVertices_Span_MatchesStreaming() {
        // Arrange — a hexagon, a pentagon and a res-0 cell
        var cells = new[] { SfIndex, Pentagon, AllResolution0Indexes[0] };
        foreach (var cell in cells) {
            var expected = cell.GetCellBoundaryVertices().ToArray();
            var buffer = new LatLng[H3GeometryExtensions.MaxCellBoundaryVertices];

            // Act
            var count = cell.GetCellBoundaryVertices(buffer);

            // Assert
            Assert.That(count, Is.EqualTo(expected.Length), $"vertex count for {cell}");
            for (var i = 0; i < count; i += 1) {
                Assert.That(buffer[i].Latitude, Is.EqualTo(expected[i].Latitude), $"lat {i} for {cell}");
                Assert.That(buffer[i].Longitude, Is.EqualTo(expected[i].Longitude), $"lng {i} for {cell}");
            }
        }
    }

    [Test]
    public void Test_GetCellBoundaryVertices_Span_TooSmall_Throws() {
        // Arrange
        var buffer = new LatLng[H3GeometryExtensions.MaxCellBoundaryVertices - 1];

        // Act / Assert
        Assert.That(() => SfIndex.GetCellBoundaryVertices(buffer), Throws.ArgumentException);
    }

    // ------------------------------------------------------------------
    // CompactCells (ReadOnlySpan input, Span output)
    // ------------------------------------------------------------------

    [Test]
    public void Test_CompactCells_Span_FullChildSet_CompactsToParent() {
        // Arrange — all children of a cell must compact back to exactly that cell
        var input = SfIndex.GetChildrenForResolution(12).ToArray();
        var expected = input.CompactCells();
        var buffer = new H3Index[input.Length];

        // Act
        var count = H3SetExtensions.CompactCells(input, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(expected.Count));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
        Assert.That(buffer.Take(count), Is.EqualTo(new[] { SfIndex }).AsCollection);
    }

    [Test]
    public void Test_CompactCells_Span_PartialSet_MatchesStreaming() {
        // Arrange — a partial (non-compactable) set is returned sorted/distinct
        var input = SfIndex.GetChildrenForResolution(11).Take(3).ToArray();
        var expected = input.CompactCells();
        var buffer = new H3Index[input.Length];

        // Act
        var count = H3SetExtensions.CompactCells(input, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(expected.Count));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Test_CompactCells_Span_MixedResolutions_MatchesStreaming() {
        // Arrange — all res-0 base cells expanded to res 2 must compact back to
        // the 122 base cells; a broad mixed-resolution parity check
        var input = GetAllCellsForResolution(2).ToArray();
        var expected = input.CompactCells();
        var buffer = new H3Index[input.Length];

        // Act
        var count = H3SetExtensions.CompactCells(input, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(expected.Count));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Test_CompactCells_Span_Duplicates_MatchesStreaming() {
        // Arrange
        var one = SfIndex.GetChildrenForResolution(11).First();
        var input = new[] { one, one, one };
        var expected = input.CompactCells();
        var buffer = new H3Index[input.Length];

        // Act
        var count = H3SetExtensions.CompactCells(input, buffer);

        // Assert
        Assert.That(count, Is.EqualTo(expected.Count));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Test_CompactCells_Span_Empty_ReturnsZero() {
        // Act
        var count = H3SetExtensions.CompactCells(ReadOnlySpan<H3Index>.Empty, Span<H3Index>.Empty);

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void Test_CompactCells_Span_TooSmall_Throws() {
        // Arrange
        var input = SfIndex.GetChildrenForResolution(11).ToArray();
        var buffer = new H3Index[input.Length - 1];

        // Act / Assert
        Assert.That(() => H3SetExtensions.CompactCells(input, buffer), Throws.ArgumentException);
    }

    // ------------------------------------------------------------------
    // UncompactCells (ReadOnlySpan input, Span output) / UncompactCellsSize
    // ------------------------------------------------------------------

    [TestCase(11)]
    [TestCase(12)]
    public void Test_UncompactCells_Span_SingleCell_MatchesStreaming(int resolution) {
        // Arrange
        var input = new[] { SfIndex };
        var expected = input.UncompactCells(resolution).ToArray();
        var size = H3SetExtensions.UncompactCellsSize(input, resolution);
        var buffer = new H3Index[size];

        // Act
        var count = H3SetExtensions.UncompactCells(input, resolution, buffer);

        // Assert
        Assert.That(size, Is.EqualTo(expected.Length), "UncompactCellsSize should equal the streamed count");
        Assert.That(count, Is.EqualTo(expected.Length));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Test_UncompactCells_Span_MultiCell_MatchesStreaming() {
        // Arrange — the full base-cell set expanded to res 2
        var input = AllResolution0Indexes.ToArray();
        var expected = input.UncompactCells(2).ToArray();
        var size = H3SetExtensions.UncompactCellsSize(input, 2);
        var buffer = new H3Index[size];

        // Act
        var count = H3SetExtensions.UncompactCells(input, 2, buffer);

        // Assert
        Assert.That(size, Is.EqualTo(expected.Length));
        Assert.That(count, Is.EqualTo(expected.Length));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Test_UncompactCells_Span_Duplicates_MatchesStreaming() {
        // Arrange — duplicate inputs are de-duplicated exactly as the stream does
        var input = new[] { SfIndex, SfIndex };
        var expected = input.UncompactCells(11).ToArray();
        var size = H3SetExtensions.UncompactCellsSize(input, 11);
        var buffer = new H3Index[size];

        // Act
        var count = H3SetExtensions.UncompactCells(input, 11, buffer);

        // Assert — size is an upper bound over all inputs; the deduped count is <=
        Assert.That(count, Is.EqualTo(expected.Length));
        Assert.That(buffer.Take(count), Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void Test_UncompactCellsSize_CellFinerThanTarget_Throws() {
        // Arrange
        var input = new[] { SfIndex };

        // Act / Assert
        Assert.That(() => H3SetExtensions.UncompactCellsSize(input, 8), Throws.ArgumentException);
    }

    [Test]
    public void Test_UncompactCells_Span_TooSmall_Throws() {
        // Arrange
        var input = new[] { SfIndex };
        var size = (int)H3SetExtensions.UncompactCellsSize(input, 11);
        var buffer = new H3Index[size - 1];

        // Act / Assert
        Assert.That(() => H3SetExtensions.UncompactCells(input, 11, buffer), Throws.ArgumentException);
    }
}
