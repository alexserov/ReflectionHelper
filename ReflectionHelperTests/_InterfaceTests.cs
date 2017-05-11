using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
#if RHELPER
using ReflectionFramework;
using ReflectionFramework.Attributes;
using ReflectionFramework.Extensions;

namespace ReflectionHelperTests
#else
using DevExpress.Xpf.Core.ReflectionExtensions.Attributes;
using DevExpress.Xpf.Core.ReflectionExtensions;
using DevExpress.Xpf.Core.Internal;

namespace DevExpress.Xpf.Core.ReflectionExtensions.Tests
#endif
    {
    public interface ISomeInterface {
        int Method();
    }

    public class SomeClass : ISomeInterface {
        int ISomeInterface.Method() {
            return Method() + 1;
        }

        int Method() {
            return 1;
        }
    }

    public interface ISomeClass {
        [InterfaceMember(typeof(ISomeInterface))]
        int Method();
    }
    [TestFixture]
    public class InterfaceTests {
        [Test]
        public void Test() {
            var sc = new SomeClass();
            var isc = sc.Wrap<ISomeClass>();
            Assert.AreEqual(2, isc.Method());
        }
    }
}
