using System;
using System.Reflection;

namespace ReflectionFramework {
    public static class ReflectionHelperAttributes {
        [AttributeUsage(AttributeTargets.Property)]
        public class FieldAccessorAttribute : Attribute {}

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Interface)]
        public class BindingFlagsAttribute : Attribute {
            readonly BindingFlags flags;
            public BindingFlags Flags { get { return flags; } }

            public BindingFlagsAttribute(BindingFlags flags) {
                this.flags = flags;
            }
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
        public class NameAttribute : Attribute {
            readonly string name;
            public string Name { get { return name; } }
            public NameAttribute(string name) {
                this.name = name;
            }
        }        
    }
}