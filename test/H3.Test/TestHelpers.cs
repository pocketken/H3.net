using System;
using H3.Model;
using NUnit.Framework;

namespace H3.Test {

    public static class TestHelpers {

        // result of encoding Point(-110, 30) @ Res14 in PG
        public const ulong TestIndexValue = 0x8e48e1d7038d527;

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
        public static readonly (ulong, int)[] TestIndexKRingsTo2 = new (ulong, int)[] {
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

        public static H3Index CreateIndex(int resolution, int baseCell, Direction direction) {
            H3Index index = new H3Index(H3Index.H3_INIT) {
                Mode = Mode.Hexagon,
                Resolution = resolution,
                Direction = direction,
                BaseCellNumber = baseCell
            };

            if (index.BaseCellNumber != baseCell) throw new Exception("ZONK");

            for (int r = 1; r <= resolution; r += 1) index.SetDirectionForResolution(r, direction);

            return index;
        }

        public static void AssertAll(H3Index[] expected, H3Index[] actual) {
            Assert.AreEqual(expected.Length, actual.Length, "should have same Length");
            for (int i = 0; i < expected.Length; i+= 1) {
                Assert.IsTrue(expected[i] == actual[i], $"index {i} should be {expected[i]} not {actual[i]}");
            }
        }

        public static void AssertAll(ulong[] expected, H3Index[] actual) {
            Assert.AreEqual(expected.Length, actual.Length, "should have same Length");
            for (int i = 0; i < expected.Length; i += 1) {
                Assert.IsTrue(expected[i] == actual[i], $"index {i} should be {expected[i]} not {actual[i]}");
            }
        }
    }
}
