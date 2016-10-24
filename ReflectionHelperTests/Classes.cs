using System;
using System.Reflection;
using ReflectionFramework;


namespace ReflectionHelperTests {
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
