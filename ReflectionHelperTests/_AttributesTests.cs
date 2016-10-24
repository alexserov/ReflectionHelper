using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ReflectionFramework;


namespace ReflectionHelperTests {
    class AttributesTestObject {
        public AttributesTestObject() {
            Prop1 = "Prop1";
            prop2 = "prop2";
        }
        public string Prop1 { get; set; }
        public string prop2;

        public string Method1() {
            return "hello";
        }
        string Method2() {
            return "world";
        }
    }

    public interface IAttributesTestObject {
        [ReflectionHelperAttributes.Name("Prop1")]
        string Prop100 { get; set; }

        [ReflectionHelperAttributes.Name("prop2")]
        [ReflectionHelperAttributes.FieldAccessor]
        string Prop12 { get; set; }

        [ReflectionHelperAttributes.Name("Method1")]
        string Method2();

        [ReflectionHelperAttributes.Name("Method2")]
        [ReflectionHelperAttributes.BindingFlags(BindingFlags.NonPublic | BindingFlags.Instance)]
        string Method3();
    }
    [TestFixture]
    public class AttributesTests {
        [Test]
        public void Test() {
            var to = new AttributesTestObject();
            var w = to.Wrap<IAttributesTestObject>();

            Assert.AreEqual("Prop1", w.Prop100);
            Assert.AreEqual("prop2", w.Prop12);
            w.Prop100 = "hello";
            w.Prop12 = "world";

            Assert.AreEqual("hello", w.Prop100);
            Assert.AreEqual("world", w.Prop12);

            Assert.AreEqual("hello", w.Method2());
            Assert.AreEqual("world", w.Method3());
        }
    }
}
