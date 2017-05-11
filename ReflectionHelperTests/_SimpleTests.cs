using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    [FallbackMode(ReflectionHelperFallbackMode.ThrowNotImplementedException)]
    public interface IClass1_1 : IClass1 {}

    [TestFixture]
    public class Tests {
       
        [Test]
        public void PublicVoidMethodTest() {
            Class1 cl = new Class1();
            var wrapped = cl.Wrap<IClass1_1>();
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
            var wrapped = cl.DefineWrapper<IClass1>().DefaultFallbackMode(ReflectionHelperFallbackMode.ThrowNotImplementedException)
                            .Create();
            wrapped.PublicStringProperty = "hello";
            Assert.AreEqual("set_PublicStringProperty", cl.LastMethod);
            Assert.AreEqual("hello", wrapped.PublicStringProperty);
            Assert.AreEqual("get_PublicStringProperty", cl.LastMethod);
        }
    }
}
