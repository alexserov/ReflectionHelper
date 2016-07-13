using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ReflectionFramework;
using NUnit.Framework.Internal;
using NUnit.Framework;

namespace ReflectionHelperTests {
    class FulentAPITestObject {
        private string stringField;
        public string PublicStringProperty { get; set; }
        public string PrivateStringProperty { get; set; }

        public string PublicMethod(string value) {
            return value;
        }

        string PrivateMethod(string value) {
            return value;            
        }
    }

    public interface IFluentAPITestObject {
        string stringField { get; set; }
        string PublicStringProperty { get; set; }
        string PrivateStringProperty { get; set; }

        string PublicMethod(string value);
        string PrivateMethod(string value);
    }

    [TestFixture]
    public class FluentAPITests {
        [Test]
        public void Simple() {
            var tc = new FulentAPITestObject();
            var ti = tc.DefineWrapper<IFluentAPITestObject>()
                .DefineProperty(x => x.PrivateStringProperty)
                    .BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)
                    .EndMember()
                .DefineProperty(x=>x.stringField)
                    .BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FieldAccessor()
                    .EndMember()
                .DefineMethod(x=>x.PrivateMethod(null))
                    .BindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)
                    .EndMember()
                .Create();
            ti.stringField = "hello";
            Assert.AreEqual("hello", ti.stringField);
        }
    }
}
