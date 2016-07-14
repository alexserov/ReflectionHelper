using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Internal;
using ReflectionFramework;
using NUnit.Framework;

namespace ReflectionHelperTests {
    
    public class SomeClass1 {
        public SomeClass1() {
            Prop1 = "Hello";
        }
        public string Prop1 { get; set; }
    }

    public class SomeClass2 {
        public SomeClass2() {
            field = new SomeClass1();
        }
        public SomeClass1 field;
    }
    [ReflectionHelperAttributes.Wrapper]
    public interface ISomeClass1 {
        string Prop1 { get; set; }
    }
    [ReflectionHelperAttributes.Wrapper]
    public interface ISomeClass2 {
        [ReflectionHelperAttributes.FieldAccessor]
        ISomeClass1 field { get; set; }
    }
    [TestFixture]
    class WrapperTests {
        [Test]
        public void FieldsTest() {
            var sc2 = new SomeClass2();
            var sc2W = sc2.Wrap<ISomeClass2>();
            Assert.AreEqual("Hello", sc2W.field.Prop1);
            sc2W.field = new SomeClass1() { Prop1 = "World" }.Wrap<ISomeClass1>();
            Assert.AreEqual("World", sc2W.field.Prop1);
        }        
    }
}
