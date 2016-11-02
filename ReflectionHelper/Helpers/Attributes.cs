using System;
using System.Reflection;

namespace ReflectionFramework.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAccessorAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Interface)]
    public class BindingFlagsAttribute : Attribute {
        readonly BindingFlags flags;

        public BindingFlags Flags {
            get { return flags; }
        }

        public BindingFlagsAttribute(BindingFlags flags) { this.flags = flags; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class NameAttribute : Attribute {
        readonly string name;

        public string Name {
            get { return name; }
        }

        public NameAttribute(string name) { this.name = name; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class InterfaceMemberAttribute : Attribute {
        public string InterfaceName { get; private set; }
        public InterfaceMemberAttribute(string interfaceTypeName) { InterfaceName = interfaceTypeName; }

        public InterfaceMemberAttribute(Type interfacType) : this(interfacType.FullName) { }
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public class WrapperAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class AssignableFromAttribute : Attribute {
        string typeName;
        public string GetTypeName() { return typeName; }
        public bool Inverse { get; set; }
        public AssignableFromAttribute(Type type) : this(type.FullName) { }

        public AssignableFromAttribute(string typeName) { this.typeName = typeName; }
    }

    public enum ReflectionHelperFallbackMode {        
        Default,
        ThrowNotImplementedException,
        FallbackWithoutValidation,
        AbortWrapping
    }
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property)]
    public class FallbackModeAttribute : Attribute {
        readonly ReflectionHelperFallbackMode mode;
        public ReflectionHelperFallbackMode Mode { get { return mode; } }
        public FallbackModeAttribute(ReflectionHelperFallbackMode mode) { this.mode = mode; }
    }
}