using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public class StaticAndPublicMergedImplementationTestObject {
        static StaticAndPublicMergedImplementationTestObject instance = new StaticAndPublicMergedImplementationTestObject();

        public static StaticAndPublicMergedImplementationTestObject Instance {
            get { return instance; }
        }

        public string Method() { return "Hello"; }
    }
    [Wrapper]
    interface IStaticAndPublicMergedImplementationTestObject {
        [BindingFlags(BindingFlags.Static|BindingFlags.Public)]
        IStaticAndPublicMergedImplementationTestObject Instance { get; }

        string Method();
    }
    [TestFixture]
    public class StaticAndPublicMergedImplementationTests
    {
        [Test]
        public void Test() { Assert.AreEqual("Hello", typeof(StaticAndPublicMergedImplementationTestObject).Wrap<IStaticAndPublicMergedImplementationTestObject>().Instance.Method()); }
    }
}
