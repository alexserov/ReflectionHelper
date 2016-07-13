using System;
using System.Reflection;

namespace ReflectionFramework {
    public static class ReflectionHelperAttributes {
        [AttributeUsage(AttributeTargets.Property)]
        public class FieldAccessorAttribute : Attribute {}

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
        public class BindingFlagsAttribute : Attribute {
            private readonly BindingFlags flags;

            public BindingFlagsAttribute(BindingFlags flags) {
                this.flags = flags;
            }
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
        public class NameAttribute : Attribute {
            private readonly string name;

            public NameAttribute(string name) {
                this.name = name;
            }
        }
        //[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
        //public class WrapResultAttribute : Attribute {
        //    private readonly Type tInterface;

        //    public WrapResultAttribute(Type tInterface) {
        //        this.tInterface = tInterface;
        //    }
        //}
    }
}