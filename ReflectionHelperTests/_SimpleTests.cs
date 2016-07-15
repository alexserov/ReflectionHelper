using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ReflectionFramework;
using Xunit;


namespace ReflectionHelperTests {
    public class Class1 {
        string publicStringProperty;
        public string LastMethod { get; set; }

        public string PublicStringProperty {
            get {
                LastMethod = "get_PublicStringProperty";
                return publicStringProperty;
            }
            set {
                LastMethod = "set_PublicStringProperty";
                publicStringProperty = value;
            }
        }

        public void PublicVoidMethod() {
            LastMethod = "PublicVoidMethod";
        }

        void PrivateVoidMethod() {
            LastMethod = "PrivateVoidMethod";
        }
    }

    public interface IClass1 {
        void PublicVoidMethod();
        void PrivateVoidMethod();
        string PublicStringProperty { get; set; }
    }
    
    public class Tests {
       
        [Fact]
        public void PublicVoidMethodTest() {
            Class1 cl = new Class1();
            var wrapped = cl.Wrap<IClass1>();
            wrapped.PublicVoidMethod();
            Assert.Equal("PublicVoidMethod", cl.LastMethod);
        }
        [Fact]
        public void PrivateVoidMethodTest() {
            Class1 cl = new Class1();
            var wrapped = cl.DefineWrapper<IClass1>()
                .DefineMethod(x => x.PrivateVoidMethod())
                .BindingFlags(BindingFlags.NonPublic | BindingFlags.Instance)
                .EndMember()
                .Create();
            wrapped.PrivateVoidMethod();
            Assert.Equal("PrivateVoidMethod", cl.LastMethod);
        }
        [Fact]
        public void PublicStringPropertyTest() {
            Class1 cl = new Class1();
            var wrapped = cl.DefineWrapper<IClass1>()
                .Create();
            wrapped.PublicStringProperty = "hello";
            Assert.Equal("set_PublicStringProperty", cl.LastMethod);
            Assert.Equal("hello", wrapped.PublicStringProperty);
            Assert.Equal("get_PublicStringProperty", cl.LastMethod);
        }
    }
}
