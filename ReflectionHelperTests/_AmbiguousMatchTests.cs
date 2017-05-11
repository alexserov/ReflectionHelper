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
    
    public class AmbiguousMatchTestClass {
        public string LastMethod { get; set; }

        public void M1() { LastMethod = "1"; }

        public void M1(string str) { LastMethod = "2"; }

        public void M1(object obj) { LastMethod = "3"; }

        public string M1(int obj) {
            LastMethod = "4";
            return "4";
        }

        public string M1(double obj) {
            LastMethod = "5";
            return "5";
        }
        public void M1(AmbiguousMatchTestClass obj) {
            LastMethod = "6";            
        }
    }
    [Wrapper, AssignableFrom("ReflectionHelperTests.AmbiguousMatchTestClass")]
    internal interface IAmbiguousMatchTestClass {
        string LastMethod { get; }

        void M1();

        void M1(string str);

        void M1(object obj);
        string M1(int obj);
        string M1(double obj);
        void M1(IAmbiguousMatchTestClass obj);
    }

    [TestFixture]
    class AmbiguousMatchTests
    {
        [Test]
        public void Test() {
            var tc = new AmbiguousMatchTestClass();
            var wtc = tc.Wrap<IAmbiguousMatchTestClass>();
            Assert.AreEqual(null, wtc.LastMethod);
            wtc.M1();
            Assert.AreEqual("1", wtc.LastMethod);
            wtc.M1("hello");
            Assert.AreEqual("2", wtc.LastMethod);
            wtc.M1(new object());
            Assert.AreEqual("3", wtc.LastMethod);
            wtc.M1(10);
            Assert.AreEqual("4", wtc.LastMethod);
            wtc.M1(10d);
            Assert.AreEqual("5", wtc.LastMethod);
            wtc.M1(wtc);
            Assert.AreEqual("6", wtc.LastMethod);
        }
    }
}
