using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using H3.Model;
using H3.Extensions;
using static H3.Constants;
using NUnit.Framework;


namespace H3.Test; 

public static class TestHelpers {

    // result of encoding Point(-110, 30) @ Res14 in PG
    public const ulong TestIndexValue = 0x8e48e1d7038d527;

    /// <summary>
    /// The single tolerance for the whole suite.  Every *calculated* double is held to
    /// within this many units-in-the-last-place (ULP) of its authoritative Uber libh3
    /// v4.5.0 reference value.  ULP is scale-free, so one bound governs every metric —
    /// cell areas, edge lengths, coordinates — with no per-metric fudging and no special
    /// case for areas.  Discrete results (indexes, counts, WKT strings) are matched exactly.
    ///
    /// 3 is the measured floor on this platform (glibc): coordinates, cell boundaries and
    /// the hexagon tables reproduce libh3 bit-for-bit (0 ULP); the residual is set solely
    /// by the whole-sphere area-sum invariant (see <see cref="SphereSumMaxUlps"/>), which
    /// is pure double accumulation, not projection fidelity, so it gets its own bound.
    /// Every per-cell quantity — coordinates, boundaries, cell areas, hexagon tables —
    /// reproduces libh3 to 0 ULP here bar one res-15 cell area at 1 ULP, so the per-cell
    /// gate sits at 1: maximally sensitive where the projection is re-optimized.
    /// </summary>
    public const long MaxUlps = 1;

    /// <summary>
    /// Separate bound for the whole-sphere cell-area sum vs 4*pi.  This is a numerical
    /// accumulation invariant (millions of Kahan-summed areas), not a per-cell parity
    /// check, so it is gated on its own rather than dragging the per-cell gate up.
    /// </summary>
    public const long SphereSumMaxUlps = 3;

    // Monotonic total ordering over IEEE-754 doubles (handles the sign bit and -0.0).
    private static ulong OrderKey(double d) {
        if (d == 0.0) d = 0.0; // collapse -0.0 to +0.0
        long bits = BitConverter.DoubleToInt64Bits(d);
        return bits >= 0
            ? (ulong)bits + 0x8000000000000000UL
            : 0x8000000000000000UL - (ulong)(-bits);
    }

    /// <summary>Distance between two doubles measured in representable steps (ULP).</summary>
    public static ulong UlpDistance(double a, double b) {
        if (double.IsNaN(a) || double.IsNaN(b) || double.IsInfinity(a) || double.IsInfinity(b))
            return a == b ? 0UL : ulong.MaxValue;
        ulong ka = OrderKey(a), kb = OrderKey(b);
        return ka >= kb ? ka - kb : kb - ka;
    }

    /// <summary>True when <paramref name="actual"/> is within <see cref="MaxUlps"/> of the authoritative <paramref name="expected"/>.</summary>
    public static bool WithinUlps(double actual, double expected) => UlpDistance(actual, expected) <= (ulong)MaxUlps;

    // Running max ULP observed per operation family, so a gate trip reads as "which
    // family moved and by how much" instead of a bare pass/fail.  Pair with the
    // environment manifest (see UlpDiagnostics) to tell a real regression from a
    // platform/runtime libm change.  Thread-safe: the suite runs fully parallel.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> MaxUlpByOpMap = new();

    /// <summary>
    /// Records the ULP distance for operation family <paramref name="op"/> (keeping the
    /// running max) and returns whether it is within <see cref="MaxUlps"/>.  Use in place
    /// of <see cref="WithinUlps"/> at reference-parity comparison sites so diagnostics accrue.
    /// </summary>
    public static bool CheckUlps(string op, double actual, double expected, long maxUlps = MaxUlps) {
        var ulp = UlpDistance(actual, expected);
        var value = ulp > long.MaxValue ? long.MaxValue : (long)ulp;
        MaxUlpByOpMap.AddOrUpdate(op, value, (_, current) => current >= value ? current : value);
        return ulp <= (ulong)maxUlps;
    }

    /// <summary>Per-operation maximum ULP observed so far this run (op family -&gt; max ULP).</summary>
    public static IReadOnlyDictionary<string, long> MaxUlpByOp => MaxUlpByOpMap;

    // uber sf test point
    public static readonly LatLng SfCoord = (0.659966917655, -2.1364398519396);
    public static readonly H3Index SfIndex = H3Index.FromLatLng(SfCoord, 9);

    // result of select h3_to_children('8e48e1d7038d527'::h3index, 15) in PG
    public static readonly ulong[] TestIndexChildrenAtRes15 = new ulong[7] {
        0x8f48e1d7038d520,
        0x8f48e1d7038d521,
        0x8f48e1d7038d522,
        0x8f48e1d7038d523,
        0x8f48e1d7038d524,
        0x8f48e1d7038d525,
        0x8f48e1d7038d526,
    };

    // Cell index values for resolutions 1 -> 14 for TestIndexValue
    public static readonly Direction[] TestIndexDirectionPerResolution = new Direction[14] {
        Direction.JK,
        Direction.I,
        Direction.K,
        Direction.IJ,
        Direction.IK,
        Direction.IJ,
        Direction.Center,
        Direction.K,
        Direction.IJ,
        Direction.K,
        Direction.IK,
        Direction.J,
        Direction.I,
        Direction.I
    };

    // select h3_k_ring_distances('8e48e1d7038d527'::h3index, 2);
    public static readonly (H3Index, int)[] TestIndexKRingsTo2 = {
        (0x8e48e1d7038d527, 0),
        (0x8e48e1d7038dc9f, 1),
        (0x8e48e1d7038d537, 1),
        (0x8e48e1d7038d507, 1),
        (0x8e48e1d7038d52f, 1),
        (0x8e48e1d7038dcd7, 1),
        (0x8e48e1d7038dc8f, 1),
        (0x8e48e1d7038dc87, 2),
        (0x8e48e1d7038dc97, 2),
        (0x8e48e1d7038c26f, 2),
        (0x8e48e1d7038c24f, 2),
        (0x8e48e1d7038d517, 2),
        (0x8e48e1d7038d51f, 2),
        (0x8e48e1d7038d50f, 2),
        (0x8e48e1d7038d577, 2),
        (0x8e48e1d7038dcdf, 2),
        (0x8e48e1d7038dcc7, 2),
        (0x8e48e1d7038dcf7, 2),
        (0x8e48e1d7038dcaf, 2),
    };

    /// <summary>
    /// Returns all of the resolution 0 base cell indexes.
    /// </summary>
    /// <returns></returns>
    public static readonly List<H3Index> AllResolution0Indexes =
        Enumerable.Range(0, NUM_BASE_CELLS)
            .Select(baseCellNumber => new H3Index {
                Mode = Mode.Cell,
                BaseCellNumber = baseCellNumber,
                Resolution = 0
            })
            .ToList();

    public static IEnumerable<H3Index> GetAllCellsForResolution(int resolution) {
        if (resolution == 0) return AllResolution0Indexes;
        return AllResolution0Indexes.UncompactCells(resolution);
    }

    public static void AssertAll(H3Index[] expected, H3Index[] actual) {
        Assert.That(actual.Length, Is.EqualTo(expected.Length), "should have same Length");
        for (var i = 0; i < expected.Length; i+= 1) {
            Assert.That(actual.Contains(expected[i]), Is.True, $"index {expected[i]} should be found");
        }
    }

    public static void AssertAll(ulong[] expected, H3Index[] actual) {
        Assert.That(actual.Length, Is.EqualTo(expected.Length), "should have same Length");
        for (var i = 0; i < expected.Length; i += 1) {
            Assert.That(expected[i] == actual[i], Is.True, $"index {i} should be {expected[i]} not {actual[i]}");
        }
    }

    public static IEnumerable<string> GetTestData(Func<string, bool> matches) {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var basePath = $"{executingAssembly.GetName().Name}.TestData";
        return executingAssembly.GetManifestResourceNames().Where(res =>
            res.StartsWith(basePath) && matches(res));
    }

    public static IEnumerable<string> ReadLines(StreamReader reader) {
        string line;
        while ((line = reader.ReadLine()) != null) {
            yield return line;
        }
    }

}