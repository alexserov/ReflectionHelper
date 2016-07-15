using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ReflectionFramework;
using Xunit;


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
    public class WrapperTests {
        [Fact]
        public void FieldsTest() {
            var sc2 = new SomeClass2();
            var sc2W = sc2.Wrap<ISomeClass2>();
            Assert.NotNull(sc2W.field);
            Assert.Equal("Hello", sc2W.field.Prop1);
            sc2W.field = new SomeClass1() { Prop1 = "World" }.Wrap<ISomeClass1>();
            Assert.Equal("World", sc2W.field.Prop1);
        }        
    }
}
