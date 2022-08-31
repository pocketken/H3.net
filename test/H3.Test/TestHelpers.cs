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
        Assert.AreEqual(expected.Length, actual.Length, "should have same Length");
        for (var i = 0; i < expected.Length; i+= 1) {
            Assert.IsTrue(actual.Contains(expected[i]), $"index {expected[i]} should be found");
        }
    }

    public static void AssertAll(ulong[] expected, H3Index[] actual) {
        Assert.AreEqual(expected.Length, actual.Length, "should have same Length");
        for (var i = 0; i < expected.Length; i += 1) {
            Assert.IsTrue(expected[i] == actual[i], $"index {i} should be {expected[i]} not {actual[i]}");
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