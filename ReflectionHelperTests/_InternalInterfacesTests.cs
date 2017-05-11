using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Internal;
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
    public class InternalInterfacesTestClass {
        public double SomeMethod() { return 10d; }
    }

    internal interface IInternalInterfacesTestClass {
        double SomeMethod();
    }
    [TestFixture]
    public class _InternalInterfacesTests
    {
        [Test]
        public void Test() {
            var sc = new InternalInterfacesTestClass();
            var isc = sc.Wrap<IInternalInterfacesTestClass>();
            Assert.AreEqual(10d, isc.SomeMethod());
        }
    }
}
