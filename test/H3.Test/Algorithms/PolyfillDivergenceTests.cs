using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using H3.Algorithms;
using NetTopologySuite.Geometries;
using static H3.Utils;

namespace H3.Test.Algorithms;

/// <summary>
/// Differential polyfill corpus: fills a matrix of shapes × resolutions with
/// <see cref="Polyfill.Fill(Geometry, int, VertexTestMode)"/> and asserts the result
/// (cell count + SHA-256 of the sorted cell set) matches upstream libh3's
/// <c>polygonToCells</c> in center-containment mode.
///
/// The shapes are the families H3.NET.Native's divergence corpus reports pocketken.H3
/// 4.0.0 diverging on (thin triangle, hole, antimeridian, concave, disjoint multipolygon)
/// plus the plain sweep box.  The expected count/hash values are generated from upstream
/// libh3 4.5.0 (h3-py / the native binding) and pinned here; the binding is never
/// referenced from the test project (it is a benchmark-only dependency).
///
/// This is the regression guard for the boundary-trace fill seeding: without it the thin
/// triangle drops cells at res 8-11 (a strict subset of the reference set).
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PolyfillDivergenceTests {

    // shape name -> polygon parts; each part is (exterior ring, hole rings), rings in
    // (latitudeDegrees, longitudeDegrees).  A multipolygon is more than one part.
    private static readonly Dictionary<string, ((double lat, double lng)[] ext, (double lat, double lng)[][] holes)[]> Shapes = new() {
        ["triangle"] = new[] {
            (new[]{ (37.813318999983238, -122.4089866999972145), (37.7866302000007224, -122.3805436999997056), (37.7198061999978478, -122.3544736999993603) },
             Array.Empty<(double, double)[]>()),
        },
        ["box"] = new[] {
            (new[]{ (37.525, -122.668), (37.525, -122.168), (38.025, -122.168), (38.025, -122.668) }, Array.Empty<(double, double)[]>()),
        },
        ["concave-L"] = new[] {
            (new[]{ (37.70, -122.50), (37.70, -122.30), (37.75, -122.30), (37.75, -122.45), (37.85, -122.45), (37.85, -122.50) }, Array.Empty<(double, double)[]>()),
        },
        ["box-with-hole"] = new[] {
            (new[]{ (37.55, -122.60), (37.55, -122.25), (37.95, -122.25), (37.95, -122.60) },
             new[]{ new[]{ (37.68, -122.48), (37.68, -122.37), (37.82, -122.37), (37.82, -122.48) } }),
        },
        ["antimeridian"] = new[] {
            (new[]{ (0.5, 179.5), (0.5, -179.5), (-0.5, -179.5), (-0.5, 179.5) }, Array.Empty<(double, double)[]>()),
        },
        ["multipolygon"] = new[] {
            (new[]{ (37.55, -122.60), (37.55, -122.50), (37.65, -122.50), (37.65, -122.60) }, Array.Empty<(double, double)[]>()),
            (new[]{ (37.80, -122.35), (37.80, -122.25), (37.90, -122.25), (37.90, -122.35) }, Array.Empty<(double, double)[]>()),
        },
    };

    // { shape, resolution, expectedCount, expectedSha256 } from upstream libh3 4.5.0.
    // Thin triangle carries res 8-11 (the regression); the heavier shapes are capped at
    // res 9 to keep the fill sizes (and suite runtime) reasonable.
    private static readonly object[] Corpus = {
        new object[] { "triangle", 4, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" },
        new object[] { "triangle", 5, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" },
        new object[] { "triangle", 6, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" },
        new object[] { "triangle", 7, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" },
        new object[] { "triangle", 8, 7, "08b9738460d5374bd51df3f6d5b87c0eb2de20c610a090d544759f0b45a0c543" },
        new object[] { "triangle", 9, 55, "8a1b20e36a8e1831f2dd2ae4b39600af382fdd02c39bf56d34f5bac6f4b694b3" },
        new object[] { "triangle", 10, 377, "f7acdeac88c209abb47a63d5bd0bca616050f5e1624b977964e8eb241d71b522" },
        new object[] { "triangle", 11, 2636, "fe11fdee514854e03a8156ff80bebf05760df18077923010ca968fc79fb33e6a" },
        new object[] { "box", 4, 1, "92fd9cb39eca8cd09cfeec851ee5b4ca09f802100a8f68b173a92efbef50d7d0" },
        new object[] { "box", 5, 9, "8b5926de8c24a3bd0856639ca1571cae715b3e9f9dba65e8bcf262f298fd770d" },
        new object[] { "box", 6, 65, "a6ee5ee928ce73a7090c1b3af9360b8541bc92378aa8d8081ed80384bb28ee59" },
        new object[] { "box", 7, 455, "4ba888ed2f45cbff40acf2d007e54e135a1d8db9aaee80e0efb841747af535a5" },
        new object[] { "box", 8, 3189, "8de22f91f66946c9021e7106f682b65131037320f9175bd51bdf7f4f3cbbbbca" },
        new object[] { "box", 9, 22334, "03f78e74976a300ab43c69cf787b4908bc9416bcaaff34f7c961bf511fc47337" },
        new object[] { "concave-L", 4, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" },
        new object[] { "concave-L", 5, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" },
        new object[] { "concave-L", 6, 4, "492b849425718317e3f7c6e0f30ad62474903b1c194f121990b168934b6e27a6" },
        new object[] { "concave-L", 7, 27, "e5d455b3be1698ab167bf6fb7746db55ab384be83d8f2edb272570c09870943e" },
        new object[] { "concave-L", 8, 190, "20171fe5f1d49daba278b94ea0216ec1f36cceeb5fb4761458a34e2417023c3d" },
        new object[] { "concave-L", 9, 1341, "a08f3d50fb8de8e746732dd9a4f1c1cd3d163588738a123c822878ac33106cb2" },
        new object[] { "box-with-hole", 4, 1, "92fd9cb39eca8cd09cfeec851ee5b4ca09f802100a8f68b173a92efbef50d7d0" },
        new object[] { "box-with-hole", 5, 5, "29711318a15167870d3b63ca0cc95834aa6f00a0a0b23662e3b622cfcaf9fd6c" },
        new object[] { "box-with-hole", 6, 33, "e171ebf5efc3cff9bed3ac4299d9b3dc8595f5bc2dbb8fecf0f0bec9e35d9d19" },
        new object[] { "box-with-hole", 7, 228, "81a4c000705ba489c9141181caf2da69bf3743bf3216ffe46d0f00b3d3a8df0a" },
        new object[] { "box-with-hole", 8, 1591, "10af12aa326804763582a7f882f6b6c54e87beaa440e5d31b4b905e8732c72ed" },
        new object[] { "box-with-hole", 9, 11130, "469fb2b52530773a26fec50bea2a96b242a80e98e28b4aec03687f075980102d" },
        new object[] { "antimeridian", 4, 8, "a6bc2b46528f52fabe0e809075b7cbaaa8201a7a27abc0cb319310ea8661c985" },
        new object[] { "antimeridian", 5, 64, "ccd5ff58da1588cde7430ea121fef29f06652a573cb23c667c4ae3b78c808251" },
        new object[] { "antimeridian", 6, 457, "4fb4098a4e6daa7c3807051837ea0c109bd9bdebefc28bab83e51c2bd823129f" },
        new object[] { "antimeridian", 7, 3212, "5b7d313d0740b39994fcf1e925d3bc3819b044571a815a6d6611af155eb7b0c8" },
        new object[] { "antimeridian", 8, 22528, "1da1575c6e42bed0c90ca694f50814fcd78d9e25cb56a98b936bbe70bc2de548" },
        new object[] { "antimeridian", 9, 157748, "428d9169b252c77e03ecacab798a0fb60d9da6b03973e56f7b5212517be71d18" },
        new object[] { "multipolygon", 4, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" },
        new object[] { "multipolygon", 5, 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" },
        new object[] { "multipolygon", 6, 5, "cd15a179e15c38bc598497ee44d7ea18ea5d268b2cf979e25082b067e74ac73d" },
        new object[] { "multipolygon", 7, 37, "be7ca3c86d45849dd955658fe73d9cfcc76decfed7fb365c899cf16630c1f155" },
        new object[] { "multipolygon", 8, 256, "21b5b0f742107b980799bf0ce882ef21b039b9680b755d91c60e10fdf150963f" },
        new object[] { "multipolygon", 9, 1787, "a61058041fc13037d033dc24e8153145662cf9c11d3bbfdc66da354464a9b44a" },
    };

    private static Polygon BuildPart((double lat, double lng)[] ext, (double lat, double lng)[][] holes) {
        LinearRing Ring((double lat, double lng)[] r) {
            var cs = r.Select(p => new Coordinate(p.lng, p.lat)).ToList();
            if (cs[0].Distance(cs[^1]) > 0) cs.Add(cs[0].Copy());
            return DefaultGeometryFactory.CreateLinearRing(cs.ToArray());
        }
        return DefaultGeometryFactory.CreatePolygon(Ring(ext), holes.Select(Ring).ToArray());
    }

    private static string SortedSetSha(IEnumerable<ulong> cells) {
        var sorted = cells.OrderBy(x => x).ToArray();
        var bytes = new byte[sorted.Length * 8];
        for (var i = 0; i < sorted.Length; i++) BitConverter.TryWriteBytes(bytes.AsSpan(i * 8), sorted[i]);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    [Test]
    [TestCaseSource(nameof(Corpus))]
    public void Test_Polyfill_MatchesUpstream(string shape, int resolution, int expectedCount, string expectedSha) {
        var parts = Shapes[shape];

        // Fill each part independently and union, matching how the reference set was
        // generated (upstream fills a GeoPolygon per part).
        var cells = new HashSet<ulong>();
        foreach (var (ext, holes) in parts) {
            foreach (var cell in BuildPart(ext, holes).Fill(resolution)) {
                cells.Add((ulong)cell);
            }
        }

        Assert.Multiple(() => {
            Assert.That(cells.Count, Is.EqualTo(expectedCount),
                $"{shape}@res{resolution}: cell COUNT differs from upstream libh3 (subset => missed cells)");
            Assert.That(SortedSetSha(cells), Is.EqualTo(expectedSha),
                $"{shape}@res{resolution}: cell SET differs from upstream libh3");
        });
    }

    // ParallelFill must return the exact same set as the sequential fill, so the whole
    // upstream-pinned corpus must also match through ParallelFill.
    [Test]
    [TestCaseSource(nameof(Corpus))]
    public void Test_ParallelFill_MatchesUpstream(string shape, int resolution, int expectedCount, string expectedSha) {
        var cells = new HashSet<ulong>();
        foreach (var (ext, holes) in Shapes[shape]) {
            foreach (var cell in BuildPart(ext, holes).ParallelFill(resolution)) {
                cells.Add((ulong)cell);
            }
        }

        Assert.Multiple(() => {
            Assert.That(cells.Count, Is.EqualTo(expectedCount),
                $"{shape}@res{resolution}: ParallelFill cell COUNT differs from upstream libh3");
            Assert.That(SortedSetSha(cells), Is.EqualTo(expectedSha),
                $"{shape}@res{resolution}: ParallelFill cell SET differs from upstream libh3");
        });
    }

    // Shape × resolution pairs (including the larger, multi-strip fills where parallelism
    // is meant to be used) at which ParallelFill must equal the sequential Fill exactly.
    private static readonly object[] EquivalenceCases = {
        new object[] { "triangle", 9 },
        new object[] { "triangle", 11 },
        new object[] { "box", 9 },
        new object[] { "box", 10 },
        new object[] { "concave-L", 10 },
        new object[] { "box-with-hole", 10 },
        new object[] { "antimeridian", 8 },
        new object[] { "multipolygon", 9 },
    };

    [Test]
    [TestCaseSource(nameof(EquivalenceCases))]
    public void Test_ParallelFill_EqualsSequentialFill(string shape, int resolution) {
        var sequential = new HashSet<ulong>();
        var parallel = new HashSet<ulong>();
        foreach (var (ext, holes) in Shapes[shape]) {
            var poly = BuildPart(ext, holes);
            foreach (var cell in poly.Fill(resolution)) sequential.Add((ulong)cell);
            foreach (var cell in poly.ParallelFill(resolution)) parallel.Add((ulong)cell);
        }

        Assert.That(parallel.SetEquals(sequential), Is.True,
            $"{shape}@res{resolution}: ParallelFill ({parallel.Count} cells) != sequential Fill ({sequential.Count} cells)");
    }

    [Test]
    public void Test_ParallelFill_EmptyPolygon_ReturnsEmpty() {
        Assert.That(DefaultGeometryFactory.CreatePolygon().ParallelFill(9), Is.Empty);
    }

    // Regardless of the requested degree of parallelism — including 1 (sequential fallback)
    // and far more strips than there are cells — the result set is unchanged.
    [Test]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(16)]
    [TestCase(64)]
    public void Test_ParallelFill_AnyDegreeOfParallelism_EqualsSequential(int degreeOfParallelism) {
        var (ext, holes) = Shapes["box"][0];
        var box = BuildPart(ext, holes);
        var sequential = new HashSet<ulong>(box.Fill(9).Select(cell => (ulong)cell));
        var parallel = new HashSet<ulong>(box.ParallelFill(9, maxDegreeOfParallelism: degreeOfParallelism).Select(cell => (ulong)cell));

        Assert.That(parallel.SetEquals(sequential), Is.True,
            $"degreeOfParallelism={degreeOfParallelism}: ParallelFill != sequential Fill");
    }
}
