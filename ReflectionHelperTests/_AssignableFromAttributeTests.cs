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

    [AssignableFrom(typeof(AFA_Class2))]
    [AssignableFrom(typeof(IAFA_Interface))]
    [AssignableFrom(typeof(Class3), Inverse = true)]
    [AssignableFrom(typeof(IAFA_Interface2), Inverse = true)]
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
