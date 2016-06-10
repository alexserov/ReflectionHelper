using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Xpf.Core.Internal;
using NUnit.Framework;

namespace ReflectionHelperTests {
    class StaticFulentAPITestObject {
        private static string stringField;
        public static string PublicStringProperty { get; set; }
        public static string PrivateStringProperty { get; set; }

        public static string PublicMethod(string value) {
            return value;
        }

        static string PrivateMethod(string value) {
            return value;
        }
    }

    public interface IStaticFluentAPITestObject {
        string stringField { get; set; }
        string PublicStringProperty { get; set; }
        string PrivateStringProperty { get; set; }

        string PublicMethod(string value);
        string PrivateMethod(string value);
    }

    [TestFixture]
    public class StaticFluentAPITests {
        [Test]
        public void Simple() {
            var ti = ReflectionGenerator.DefineWrapper<StaticFulentAPITestObject, IFluentAPITestObject>()
                .DefineProperty(x => x.PrivateStringProperty)
                    .BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)
                    .EndMember()
                .DefineProperty(x => x.stringField)
                    .BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FieldAccessor()
                    .EndMember()
                .DefineMethod(x => x.PrivateMethod(null))
                    .BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)
                    .EndMember()
                .Create();
            Assert.AreEqual("hello", ti.PublicMethod("hello"));
            ti.stringField = "hello";
            Assert.AreEqual("hello", ti.stringField);
        }
    }
}
