using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ReflectionFramework;
using Xunit;

namespace ReflectionHelperTests {
    public interface IParentInterface : IParentInterface2 {
        string Prop1 { get; }
        string Method1();
    }

    public interface IParentInterface2 {
        
    }

    public interface IChildInterface : IParentInterface {
        string Prop2 { get; }
        string Method2();
    }

    public class InheritedInterfacesTestClass {
        public InheritedInterfacesTestClass() {
            Prop1 = "Hello";
            Prop2 = "World";
        }
        public string Prop1 { get; set; }
        public string Prop2 { get; set; }

        public string Method1() {
            return Prop1;
        }

        public string Method2() {
            return Prop2;
        }
    }


    public class InheritedInterfacesTests {
        [Fact]
        public void Test() {
            var tc = new InheritedInterfacesTestClass();
            var w = tc.Wrap<IChildInterface>();
            Assert.Equal("Hello", w.Prop1);
            Assert.Equal("World", w.Prop2);
            Assert.Equal("Hello", w.Method1());
            Assert.Equal("World", w.Method2());
        }
    }
}
