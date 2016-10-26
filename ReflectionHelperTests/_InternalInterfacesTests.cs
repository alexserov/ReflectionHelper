using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Internal;
using NUnit.Framework;
using ReflectionFramework;
using ReflectionFramework.Extensions;

namespace ReflectionHelperTests
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
