using System.Linq;
using H3.Model;
using NUnit.Framework;


namespace H3.Test.Model; 

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CoordIJTests {

    [Test]
    public void Test_Upstream_IJKToIJ_Zero() {
        // Arrange
        CoordIJK ijk = new(0, 0, 0);

        // Act
        var actual = CoordIJ.FromCoordIJK(ijk);

        // Assert
        Assert.That(actual.I, Is.EqualTo(0), "should be zero");
        Assert.That(actual.J, Is.EqualTo(0), "should be zero");
    }

    [Test]
    public void Test_Upstream_IJToIJK_Zero() {
        // Arrange
        CoordIJ ij = new(0, 0);

        // Act
        var actual = ij.ToCoordIJK();

        // Assert
        Assert.That(actual.I, Is.EqualTo(0), "should be zero");
        Assert.That(actual.J, Is.EqualTo(0), "should be zero");
        Assert.That(actual.K, Is.EqualTo(0), "should be zero");
    }

    [Test]
    public void Test_Upstream_IJKToIJ_Roundtrip() {
        // Arrange
        var coords = Enumerable.Range((int)Direction.Center, (int)Direction.Invalid)
            .Select(dir => new CoordIJK().ToNeighbour((Direction)dir));

        // Act
        var actual = coords.Select(ijk => CoordIJ.FromCoordIJK(ijk).ToCoordIJK());

        // Assert
        Assert.That(actual, Is.EqualTo(coords), "should be equal");
    }

}