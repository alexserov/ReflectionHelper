using System;
using System.Linq;
using System.Reflection;
using ReflectionFramework.Internal;

namespace ReflectionFramework {
    internal abstract class BaseReflectionHelperInterfaceWrapperSetting {
        readonly BaseReflectionHelperInterfaceWrapperGenerator reflectionHelperInterfaceWrapperGenerator;

        public BaseReflectionHelperInterfaceWrapperSetting(BaseReflectionHelperInterfaceWrapperGenerator reflectionHelperInterfaceWrapperGenerator) {
            this.reflectionHelperInterfaceWrapperGenerator = reflectionHelperInterfaceWrapperGenerator;
        }

        TAttribute GetAttribute<TAttribute>() {
            return reflectionHelperInterfaceWrapperGenerator.tWrapper.GetCustomAttributes(typeof(TAttribute), true).OfType<TAttribute>().FirstOrDefault();
        }
        TAttribute GetAttribute<TAttribute>(MemberInfo memberInfo) {
            if (memberInfo == null)
                return default(TAttribute);
            return memberInfo.GetCustomAttributes(typeof(TAttribute), true).OfType<TAttribute>().FirstOrDefault();
        }

        internal virtual BindingFlags GetBindingFlags(MemberInfo memberInfo) {            
            var attr = GetAttribute<ReflectionHelperAttributes.BindingFlagsAttribute>(memberInfo) ?? GetAttribute<ReflectionHelperAttributes.BindingFlagsAttribute>();
            if (attr != null)
                return attr.Flags;
            if (GetAttribute<ReflectionHelperAttributes.InterfaceMemberAttribute>(memberInfo) != null)
                return BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            return reflectionHelperInterfaceWrapperGenerator.defaultFlags;
        }

        internal virtual bool GetIsInterface(string name, MemberInfo memberInfo) {
            return GetAttribute<ReflectionHelperAttributes.InterfaceMemberAttribute>(memberInfo)!=null;
        }
        internal virtual string GetName(string defaultName, MemberInfo memberInfo) {
            var attr = GetAttribute<ReflectionHelperAttributes.NameAttribute>(memberInfo);
            if (attr != null)
                return attr.Name;
            
            return defaultName;
        }

        internal virtual bool FieldAccessor(MemberInfo memberInfo) {
            var attr = GetAttribute<ReflectionHelperAttributes.FieldAccessorAttribute>(memberInfo);
            if (attr != null)
                return true;
            return false;
        }

        internal virtual Delegate GetFallback(MemberInfoKind kind) {
            return new Action(() => { });
        }

        public abstract int ComputeKey();
    }

    internal class ReflectionHelperInterfaceWrapperSetting : BaseReflectionHelperInterfaceWrapperSetting {
        public ReflectionHelperInterfaceWrapperSetting(BaseReflectionHelperInterfaceWrapperGenerator reflectionHelperInterfaceWrapperGenerator)
            : base(reflectionHelperInterfaceWrapperGenerator) {}

        public BindingFlags? BindingFlags { get; set; }
        public string Name { get; set; }
        public bool IsField { get; set; }
        public Delegate FallbackAction { get; set; }
        public Delegate GetterFallbackAction { get; set; }
        public Delegate SetterFallbackAction { get; set; }
        public string InterfaceName { get; set; }

        internal override BindingFlags GetBindingFlags(MemberInfo wrapperMethodInfo) {
            return BindingFlags ?? base.GetBindingFlags(wrapperMethodInfo);
        }

        internal override string GetName(string defaultName, MemberInfo memberInfo) {
            string prefix = "";
            if (!string.IsNullOrEmpty(InterfaceName)) {
                prefix = InterfaceName + ".";
            }
            return prefix + (Name ?? base.GetName(defaultName, memberInfo));
        }

        internal override bool FieldAccessor(MemberInfo memberInfo) {
            return IsField;
        }

        internal override Delegate GetFallback(MemberInfoKind infoKind) {
            Delegate result = null;
            switch (infoKind) {
                case MemberInfoKind.Method:
                    result = FallbackAction;
                    break;
                case MemberInfoKind.PropertyGetter:
                    result = GetterFallbackAction;
                    break;
                case MemberInfoKind.PropertySetter:
                    result = SetterFallbackAction;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(infoKind), infoKind, null);
            }
            return result ?? base.GetFallback(infoKind);
        }

        public override int ComputeKey() {
            unchecked {
                var hashCode = BindingFlags.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsField.GetHashCode();
                hashCode = (hashCode * 397) ^ (FallbackAction != null ? FallbackAction.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (GetterFallbackAction != null ? GetterFallbackAction.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SetterFallbackAction != null ? SetterFallbackAction.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (InterfaceName != null ? InterfaceName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    internal class NullReflectionHelperInterfaceWrapperSetting : BaseReflectionHelperInterfaceWrapperSetting {
        public NullReflectionHelperInterfaceWrapperSetting(BaseReflectionHelperInterfaceWrapperGenerator reflectionHelperInterfaceWrapperGenerator) : base(reflectionHelperInterfaceWrapperGenerator) {}
        public override int ComputeKey() {
            return 0;
        }
    }
}