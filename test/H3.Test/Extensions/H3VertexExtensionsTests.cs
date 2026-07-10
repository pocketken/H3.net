using System.Linq;
using H3.Extensions;
using H3.Model;
using static H3.Constants;
using NUnit.Framework;


namespace H3.Test.Extensions; 

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class H3VertexExtensionsTests {

    [Test]
    public void Test_Upstream_VertexNumForDirection_Hexagon() {
        // Arrange
        var origin = new H3Index(0x823d6ffffffffff);

        // Act
        var vertexNums = Enumerable.Range((int)Direction.K, 6)
            .Select(dir => origin.GetVertexNumberForDirection((Direction)dir))
            .GroupBy(e => e)
            .Select(group => (Vertex: group.Key, Count: group.Count()));

        // Assert
        Assert.That(vertexNums.Where(e => e.Vertex == -1), Is.Empty, "should not return invalid vertex");
        Assert.That(vertexNums.Where(e => e.Count > 1), Is.Empty, "should not return the same vertex more than once");
        Assert.That(vertexNums.Where(e => e.Vertex >= NUM_HEX_VERTS), Is.Empty, "should not return vertex >= NUM_HEX_VERTS");
    }

    [Test]
    public void Test_Upstream_VertexNumForDirection_Pentagon() {
        // Arrange
        var origin = new H3Index(0x823007fffffffff);

        // Act
        var vertexNums = Enumerable.Range((int)Direction.J, 5)
            .Select(dir => origin.GetVertexNumberForDirection((Direction)dir))
            .GroupBy(e => e)
            .Select(group => (Vertex: group.Key, Count: group.Count()));

        // Assert
        Assert.That(vertexNums.Where(e => e.Vertex == -1), Is.Empty, "should not return invalid vertex");
        Assert.That(vertexNums.Where(e => e.Count > 1), Is.Empty, "should not return the same vertex more than once");
        Assert.That(vertexNums.Where(e => e.Vertex >= NUM_PENT_VERTS), Is.Empty, "should not return vertex >= NUM_PENT_VERTS");
    }

    [Test]
    [TestCase(Direction.Center)]
    [TestCase(Direction.Invalid)]
    [TestCase(Direction.K)]
    public void Test_Upstream_VertexNumForDirection_BadDirections_Pentagon(Direction direction) {
        // Arrange
        var origin = new H3Index(0x823007fffffffff);

        // Act
        var vertexNum = origin.GetVertexNumberForDirection(direction);

        // Assert
        Assert.That(vertexNum, Is.EqualTo(-1), "should return invalid vertex");
    }

    [Test]
    public void Test_Upstream_DirectionForVertexNum_Hexagon() {
        // Arrange
        var origin = new H3Index(0x823d6ffffffffff);

        // Act
        var directions = Enumerable.Range(0, NUM_HEX_VERTS)
            .Select(vertex => origin.GetDirectionForVertexNumber(vertex))
            .GroupBy(e => e)
            .Select(group => (Direction: group.Key, Count: group.Count()))
            .ToArray();

        // Assert
        Assert.That(directions.Length, Is.EqualTo(NUM_HEX_VERTS), "should return NUM_HEX_VERTS");
        Assert.That(directions.Where(e => e.Direction == Direction.Center), Is.Empty, "should not return center direction");
        Assert.That(directions.Where(e => e.Direction == Direction.Invalid), Is.Empty, "should not return invalid direction");
        Assert.That(directions.Where(e => e.Count > 1), Is.Empty, "should not return the same direction more than once");
    }

    [Test]
    public void Test_DirectionForVertexNum_Pentagon() {
        // Arrange
        var origin = new H3Index(0x823007fffffffff);

        // Act
        var directions = Enumerable.Range(0, NUM_PENT_VERTS)
            .Select(vertex => origin.GetDirectionForVertexNumber(vertex))
            .GroupBy(e => e)
            .Select(group => (Direction: group.Key, Count: group.Count()))
            .ToArray();

        // Assert
        Assert.That(directions.Length, Is.EqualTo(NUM_PENT_VERTS), "should return NUM_PENT_VERTS");
        Assert.That(directions.Where(e => e.Direction == Direction.Center), Is.Empty, "should not return center direction");
        Assert.That(directions.Where(e => e.Direction == Direction.Invalid), Is.Empty, "should not return invalid direction");
        Assert.That(directions.Where(e => e.Direction == Direction.K), Is.Empty, "should not return K direction");
        Assert.That(directions.Where(e => e.Count > 1), Is.Empty, "should not return the same direction more than once");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(6)]
    public void Test_Upstream_DirectionForVertexNum_BadVertices_Hexagon(int vertexNum) {
        // Arrange
        var origin = new H3Index(0x823d6ffffffffff);

        // Act
        var direction = origin.GetDirectionForVertexNumber(vertexNum);

        // Assert
        Assert.That(direction, Is.EqualTo(Direction.Invalid), "should return invalid direction");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(5)]
    public void Test_Upstream_DirectionForVertexNum_BadVertices_Pentagon(int vertexNum) {
        // Arrange
        var origin = new H3Index(0x823007fffffffff);

        // Act
        var direction = origin.GetDirectionForVertexNumber(vertexNum);

        // Assert
        Assert.That(direction, Is.EqualTo(Direction.Invalid), "should return invalid direction");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(6)]
    public void Test_Upstream_CellToVertex_BadVerts_Hexagon(int vertexNum) {
        // Arrange
        var origin = new H3Index(0x823d6ffffffffff);

        // Act
        var index = origin.CellToVertex(vertexNum);

        // Assert
        Assert.That(index, Is.EqualTo(H3Index.Invalid), "should return H3_NULL");
    }

    [Test]
    [TestCase(-1)]
    [TestCase(5)]
    public void Test_Upstream_CellToVertex_BadVerts_Pentagon(int vertexNum) {
        // Arrange
        var origin = new H3Index(0x823007fffffffff);

        // Act
        var index = origin.CellToVertex(vertexNum);

        // Assert
        Assert.That(index, Is.EqualTo(H3Index.Invalid), "should return H3_NULL");
    }

    [Test]
    public void Test_Upstream_IsValidVertex() {
        // Arrange
        H3Index vert = 0x2222597fffffffff;

        // Act
        var isValid = vert.IsValidVertex();

        // Assert
        Assert.That(isValid, Is.EqualTo(true), "should be valid vertex index");
    }

    [Test]
    public void Test_Upstream_GetVertexIndicies_Hex() {
        // Arrange
        H3Index origin = 0x823d6ffffffffff;

        // Act
        var indicies = origin.CellToVertexes().ToArray();

        // Assert
        Assert.That(indicies.Length, Is.EqualTo(NUM_HEX_VERTS), "should return NUM_HEX_VERTS indicies");
        Assert.That(indicies.All(index => index.IsValidVertex()), Is.True, "should return valid vertex indicies");
    }

    [Test]
    public void Test_Upstream_IsValidVertex_InvalidOwner() {
        // Arrange
        H3Index origin = 0x823d6ffffffffff;
        var vert = origin.CellToVertex(0);

        // Act
        vert ^= 1;

        // Assert
        Assert.That(vert.IsValidVertex(), Is.False, "should not be valid");
    }

    [Test]
    public void Test_Upstream_IsValidVertex_OriginDoesNotOwnCanonicalVertex() {
        // Arrange
        H3Index origin = 0x823d6ffffffffff;
        var vert = origin.CellToVertex(0);

        // Act
        H3Index owner = new(vert) {
            Mode = Mode.Cell,
            ReservedBits = 0
        };

        // Assert
        Assert.That(origin == owner, Is.False, "origin should not own canonical vertex");
    }

    [Test]
    public void Test_Upstream_IsValidVertex_WrongOwner() {
        // Arrange
        H3Index origin = 0x823d6ffffffffff;

        // Act
        H3Index nonCanonical = new(origin) {
            Mode = Mode.Vertex,
            ReservedBits = 0
        };

        // Assert
        Assert.That(nonCanonical.IsValidVertex(), Is.False, "vertex with incorrect owner should not be valid");
    }

}