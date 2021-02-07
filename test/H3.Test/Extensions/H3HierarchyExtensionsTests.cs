using System;
using System.Linq;
using H3.Extensions;
using NUnit.Framework;

namespace H3.Test.Extensions {

    [TestFixture]
    public class H3HierarchyExtensionsTests {

        // TODO copy relevant tests from upstream

        [Test]
        public void Test_KnownIndexValue_Children() {
            // Arrange
            H3Index h3 = new H3Index(TestHelpers.TestIndexValue);

            // Act
            H3Index[] children = h3.GetChildrenAtResolution(15).ToArray();

            // Assert
            TestHelpers.AssertAll(TestHelpers.TestIndexChildrenAtRes15, children);
        }

    }

}
