using System;
using System.Reflection;
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
    public enum Visibility {
        Collapsed,
        Visible,
        Hidden
    }

    public class Fakes {
        public void Method() {
            Method2(new object(), typeof(string));
        }
        public  void Method2(object obj, Type t) { }
        //public Fakes() { ReflectionFramework.ReflectionHelperExtensions }
    }
}
