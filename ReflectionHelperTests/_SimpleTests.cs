using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Xpf.Core.Internal;
using NUnit.Framework;

namespace ReflectionHelperTests {
    public class Class1 {
        private string publicStringProperty;
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

    public interface IObject {
        string Property { get; set; }
        void Method();
    }

    [TestFixture]
    class Tests {
        [Test]
        public void FallbackTest() {
            var wrapper = new object().DefineWrapper<IObject>()
                .DefineProperty(x => x.Property)
                .GetterFallback(new Func<object, string>(x => "hello"))
                .SetterFallback(new Action<object>(x => { }))
                .EndMember()
                .DefineMethod(x => x.Method())
                .Fallback(new Action<object>(x => { }))
                .EndMember()
                .Create();
            Assert.AreEqual("hello", wrapper.Property);
            wrapper.Method();
        }
        [Test]
        public void PublicVoidMethodTest() {
            Class1 cl = new Class1();
            var wrapped = cl.Wrap<IClass1>();
            wrapped.PublicVoidMethod();
            Assert.AreEqual("PublicVoidMethod", cl.LastMethod);
        }
        [Test]
        public void PrivateVoidMethodTest() {
            Class1 cl = new Class1();
            var wrapped = cl.DefineWrapper<IClass1>()
                .DefineMethod(x => x.PrivateVoidMethod())
                .BindingFlags(BindingFlags.NonPublic | BindingFlags.Instance)
                .EndMember()
                .Create();
            wrapped.PrivateVoidMethod();
            Assert.AreEqual("PrivateVoidMethod", cl.LastMethod);
        }
        [Test]
        public void PublicStringPropertyTest() {
            Class1 cl = new Class1();
            var wrapped = cl.DefineWrapper<IClass1>()
                .Create();
            wrapped.PublicStringProperty = "hello";
            Assert.AreEqual("set_PublicStringProperty", cl.LastMethod);
            Assert.AreEqual("hello", wrapped.PublicStringProperty);
            Assert.AreEqual("get_PublicStringProperty", cl.LastMethod);
        }
    }
}
