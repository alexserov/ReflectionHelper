using System;
using System.Linq;
using System.Reflection;
using ReflectionFramework.Internal;

namespace ReflectionFramework {
    internal class BaseReflectionGeneratorInstanceSetting {
        readonly BaseReflectionGeneratorInstance reflectionGeneratorInstance;

        public BaseReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance) {
            this.reflectionGeneratorInstance = reflectionGeneratorInstance;
        }

        TAttribute GetAttribute<TAttribute>() {
            return reflectionGeneratorInstance.tWrapper.GetCustomAttributes(typeof(TAttribute), true).OfType<TAttribute>().FirstOrDefault();
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
            return reflectionGeneratorInstance.defaultFlags;
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
    }

    internal class ReflectionGeneratorInstanceSetting : BaseReflectionGeneratorInstanceSetting {
        public ReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance)
            : base(reflectionGeneratorInstance) {}

        public BindingFlags? BindingFlags { get; set; }
        public string Name { get; set; }
        public bool IsField { get; set; }
        public Delegate FallbackAction { get; set; }
        public Delegate GetterFallbackAction { get; set; }
        public Delegate SetterFallbackAction { get; set; }

        internal override BindingFlags GetBindingFlags(MemberInfo wrapperMethodInfo) {
            return BindingFlags ?? base.GetBindingFlags(wrapperMethodInfo);
        }

        internal override string GetName(string defaultName, MemberInfo memberInfo) {
            return Name ?? base.GetName(defaultName, memberInfo);
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
    }

    internal class NullReflectionGeneratorInstanceSetting : BaseReflectionGeneratorInstanceSetting {
        public NullReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance) : base(reflectionGeneratorInstance) {}
    }
}