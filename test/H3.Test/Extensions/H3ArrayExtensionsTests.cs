using System;
using NUnit.Framework;
using H3.Extensions;
using System.Linq;

namespace H3.Test.Extensions {

    [TestFixture]
    public class H3ArrayExtensionsTests {

        // select h3_compact(array(select h3_k_ring('8e48e1d7038d527'::h3index, 2)));
        public static readonly H3Index[] TestCompactArray = new H3Index[] {
            0x8e48e1d7038dc9f,
            0x8e48e1d7038dcd7,
            0x8e48e1d7038dc8f,
            0x8e48e1d7038dc87,
            0x8e48e1d7038dc97,
            0x8e48e1d7038c26f,
            0x8e48e1d7038c24f,
            0x8e48e1d7038d577,
            0x8e48e1d7038dcdf,
            0x8e48e1d7038dcc7,
            0x8e48e1d7038dcf7,
            0x8e48e1d7038dcaf,
            0x8d48e1d7038d53f
        };

        // select h3_uncompact(array(select h3_compact(array(select h3_k_ring('8e48e1d7038d527'::h3index, 2)))), 14);
        public static readonly H3Index[] TestUncompactArray = new H3Index[] {
            0x8e48e1d7038dc9f,
            0x8e48e1d7038dcd7,
            0x8e48e1d7038dc8f,
            0x8e48e1d7038dc87,
            0x8e48e1d7038dc97,
            0x8e48e1d7038c26f,
            0x8e48e1d7038c24f,
            0x8e48e1d7038d577,
            0x8e48e1d7038dcdf,
            0x8e48e1d7038dcc7,
            0x8e48e1d7038dcf7,
            0x8e48e1d7038dcaf,
            0x8e48e1d7038d507,
            0x8e48e1d7038d50f,
            0x8e48e1d7038d517,
            0x8e48e1d7038d51f,
            0x8e48e1d7038d527,
            0x8e48e1d7038d52f,
            0x8e48e1d7038d537
        };

        [Test]
        public void Test_Compact_MatchesPg() {
            // Act
            var result = TestHelpers.TestIndexKRingsTo2.Select(e => (H3Index)e.Item1).ToArray().Compact();

            // Assert
            TestHelpers.AssertAll(TestCompactArray, result);
        }

        [Test]
        public void Test_Uncomapct_MatchesPg() {
            // Act
            var result = TestCompactArray.UncompactToResolution(14);

            // Assert
            TestHelpers.AssertAll(TestUncompactArray, result);
        }
    }
}
