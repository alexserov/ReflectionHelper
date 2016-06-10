using System;
using System.Reflection;

namespace DevExpress.Xpf.Core.Internal {
    public class BaseReflectionGeneratorInstanceSetting {
        private readonly BaseReflectionGeneratorInstance reflectionGeneratorInstance;

        public BaseReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance) {
            this.reflectionGeneratorInstance = reflectionGeneratorInstance;
        }

        internal virtual BindingFlags GetBindingFlags() {
            return reflectionGeneratorInstance.defaultFlags;
        }

        internal virtual string GetName(string defaultName) {
            return defaultName;
        }

        internal virtual bool FieldAccessor() {
            return false;
        }

        internal virtual Delegate GetFallback(MemberInfoKind kind) {
            return new Action(() => { });
        }
    }

    public class ReflectionGeneratorInstanceSetting : BaseReflectionGeneratorInstanceSetting {
        public ReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance)
            : base(reflectionGeneratorInstance) {}

        public BindingFlags? BindingFlags { get; set; }
        public string Name { get; set; }
        public bool IsField { get; set; }
        public Delegate FallbackAction { get; set; }
        public Delegate GetterFallbackAction { get; set; }
        public Delegate SetterFallbackAction { get; set; }

        internal override BindingFlags GetBindingFlags() {
            return BindingFlags ?? base.GetBindingFlags();
        }

        internal override string GetName(string defaultName) {
            return Name ?? base.GetName(defaultName);
        }

        internal override bool FieldAccessor() {
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

    public class NullReflectionGeneratorInstanceSetting : BaseReflectionGeneratorInstanceSetting {
        public NullReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance) : base(reflectionGeneratorInstance) {}
    }
}