using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ReflectionFramework;
using ReflectionFramework.Extensions;


namespace ReflectionHelperTests {
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
        [ReflectionFramework.Attributes.InterfaceMember(typeof(ISomeInterface))]
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
