using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ReflectionFramework;


namespace ReflectionHelperTests {
    public class AFA_Class1 {
        public string Prop1 { get; set; }
    }

    public class AFA_Class2 : AFA_Class1 {}

    public class AFA_Class3 : AFA_Class2 {}

    public class AFA_Class4 : AFA_Class1, IAFA_Interface {}

    public class AFA_Class5 : AFA_Class2, IAFA_Interface {}

    public class AFA_Class6 : AFA_Class2 {}

    public class AFA_Class7 : AFA_Class2, IAFA_Interface2 {}

    public interface IAFA_Interface {}

    public interface IAFA_Interface2 {}

    [ReflectionHelperAttributes.AssignableFrom(typeof(AFA_Class2))]
    [ReflectionHelperAttributes.AssignableFrom(typeof(IAFA_Interface))]
    [ReflectionHelperAttributes.AssignableFrom(typeof(Class3), Inverse = true)]
    [ReflectionHelperAttributes.AssignableFrom(typeof(IAFA_Interface2), Inverse = true)]
    public interface IAFAWrapper {
        string Prop1 { get; set; }
    }
    [TestFixture]
    public class AssignableFromAttributeTests {
        [Test]
        public void Test() {
            Assert.Null(new AFA_Class1().Wrap<IAFAWrapper>());
            Assert.Null(new AFA_Class2().Wrap<IAFAWrapper>());
            Assert.Null(new AFA_Class3().Wrap<IAFAWrapper>());
            Assert.Null(new AFA_Class4().Wrap<IAFAWrapper>());
            Assert.NotNull(new AFA_Class5().Wrap<IAFAWrapper>());
            Assert.Null(new AFA_Class6().Wrap<IAFAWrapper>());
            Assert.Null(new AFA_Class7().Wrap<IAFAWrapper>());
        }
    }
}
