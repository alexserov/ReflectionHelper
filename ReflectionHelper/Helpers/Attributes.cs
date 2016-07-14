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

            public string Name {
                get { return name; }
            }

            public NameAttribute(string name) {
                this.name = name;
            }
        }
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
        public class InterfaceMemberAttribute : Attribute {
            public string InterfaceName { get; }
            public InterfaceMemberAttribute(string interfaceTypeName) {
                InterfaceName = interfaceTypeName;
            }

            public InterfaceMemberAttribute(Type interfacType) : this(interfacType.FullName) {}
        }

        [AttributeUsage(AttributeTargets.Interface)]
        public class WrapperAttribute : Attribute { }
        [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
        public class AssignableFromAttribute : Attribute {
            public Type Type { get; private set; }
            public string TypeName { get; private set; }
            public bool Inverse { get; set; }
            public AssignableFromAttribute(Type type) {
                Type = type;
            }
            public AssignableFromAttribute(string typeName) {
                TypeName = typeName;
            }
        }
    }
}