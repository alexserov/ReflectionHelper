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
    public interface IObject {
        string Property { get; set; }
        void Method();
        void Method2(ref string param);
    }    
    [TestFixture]
    public class FallbackTests {
        [Test]
        public void FallbackTest() {
            string fake = null;
            var wrapper = new object().DefineWrapper<IObject>()
                .DefineProperty(x => x.Property)
                .GetterFallback(new Func<object, string>(x => "hello"))
                .SetterFallback(new Action<object, object>((i, x) => { }))
                .EndMember()
                .DefineMethod(x => x.Method())
                .Fallback(new Action<object>(x => { }))
                .EndMember()
                .DefineMethod(x=>x.Method2(ref fake))
                .Fallback(new Func<object, object, Tuple<string>>((x, a) => new Tuple<string>("abc")))
                .EndMember()                       
                .Create();
            Assert.AreEqual("hello", wrapper.Property);
            string str = "def";
            wrapper.Method();
            wrapper.Method2(ref str);
            Assert.AreEqual("abc", str);
        }
    }
}
