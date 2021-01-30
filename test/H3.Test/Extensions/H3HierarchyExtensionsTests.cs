using System;
using System.Linq;
using H3.Extensions;
using NUnit.Framework;

namespace H3.Test.Extensions {

    [TestFixture]
    public class H3HierarchyExtensionsTests {

        [Test]
        public void Test_KnownIndexValue_Children() {
            // Arrange
            H3Index h3 = new H3Index(TestHelpers.TestIndexValue);

            // Act
            H3Index[] children = h3.GetChildrenAtResolution(15).ToArray();

            // Assert
            AssertChildren(TestHelpers.TestIndexChildrenAtRes15, children);
        }

        private static void AssertChildren(ulong[] expectedChildren, H3Index[] actualChildren) {
            Assert.AreEqual(expectedChildren.Length, actualChildren.Length, "should have same length");
            for (int i = 0; i < expectedChildren.Length; i += 1) {
                Assert.IsTrue(expectedChildren[i] == actualChildren[i], "should be same child");
            }
        }

    }

}
