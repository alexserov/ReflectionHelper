using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ReflectionFramework;
using Xunit;

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
        [ReflectionHelperAttributes.InterfaceMember(typeof(ISomeInterface))]
        int Method();
    }

    public class InterfaceTests {
        [Fact]
        public void Test() {
            var sc = new SomeClass();
            var isc = sc.Wrap<ISomeClass>();
            Assert.Equal(2, isc.Method());
        }
    }
}
