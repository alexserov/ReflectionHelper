using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ReflectionFramework;



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
        public SomeClass1 Property { get { return field; } }
        public SomeClass1 Method() { return field;  }
        public void Method2(SomeClass1 value) {
            field = value;
        }

        public SomeClass1 Method3(SomeClass1 value) {
            try {
                return field;
            }
            finally {
                field = value;
            }
        }

        public void Method4(out SomeClass1 value) {
            value = new SomeClass1() {Prop1 = "Method4"};
        }
        public string Method5(out SomeClass1 value) {
            value = new SomeClass1() { Prop1 = "Method5" };
            return Property.Prop1;
        }
        public void Method6(ref SomeClass1 value) {
            value.Prop1 = "Method6";
        }
        public string Method7(ref SomeClass1 value) {                        
            value.Prop1 = "Method7";
            return Property.Prop1;
        }
    }
    [ReflectionHelperAttributes.Wrapper]
    public interface ISomeClass1 {
        string Prop1 { get; set; }
    }

    [ReflectionHelperAttributes.Wrapper]
    public interface ISomeClass2 {
        [ReflectionHelperAttributes.FieldAccessor]
        ISomeClass1 field { get; set; }

        ISomeClass1 Property { get; }
        ISomeClass1 Method();
        void Method2(ISomeClass1 value);
        ISomeClass1 Method3(ISomeClass1 value);
        void Method4(out ISomeClass1 value);
        string Method5(out ISomeClass1 value);
        void Method6(ref ISomeClass1 value);
        string Method7(ref ISomeClass1 value);
    }
    [TestFixture]
    public class WrapperTests {
        [Test]
        public void FieldsTest() {
            var sc2 = new SomeClass2();
            var sc2W = sc2.Wrap<ISomeClass2>();
            Assert.NotNull(sc2W.field);
            Assert.AreEqual("Hello", sc2W.field.Prop1);
            sc2W.field = new SomeClass1() {Prop1 = "World"}.Wrap<ISomeClass1>();
            Assert.AreEqual("World", sc2W.field.Prop1);
            Assert.AreEqual("World", sc2W.Property.Prop1);
            Assert.AreEqual("World", sc2W.Method().Prop1);
            sc2W.Method2(new SomeClass1() {Prop1 = "Value1"}.Wrap<ISomeClass1>());
            Assert.AreEqual("Value1", sc2W.Property.Prop1);
            var old = sc2W.Method3(new SomeClass1() {Prop1 = "Value2"}.Wrap<ISomeClass1>());
            Assert.AreEqual("Value1", old.Prop1);
            Assert.AreEqual("Value2", sc2W.Property.Prop1);
        }
        [Test]
        public void OutTest() {
            var sc2 = new SomeClass2();
            var sc2W = sc2.Wrap<ISomeClass2>();
            ISomeClass1 value;
            sc2W.Method4(out value);
            Assert.AreEqual("Method4", value.Prop1);
            
            Assert.AreEqual("Hello", sc2W.Method5(out value));
            Assert.AreEqual("Method5", value.Prop1);
        }
        [Test]
        public void RefTest() {
            var sc2 = new SomeClass2();
            var sc2W = sc2.Wrap<ISomeClass2>();
            ISomeClass1 value = new SomeClass1().Wrap<ISomeClass1>();
            sc2W.Method6(ref value);
            Assert.AreEqual("Method6", value.Prop1);

            Assert.AreEqual("Hello", sc2W.Method7(ref value));
            Assert.AreEqual("Method7", value.Prop1);
        }
    }
}
