using System.Reflection;

namespace DevExpress.Xpf.Core.Internal {
    public class BaseReflectionGeneratorInstanceSetting {
        private readonly BaseReflectionGeneratorInstance reflectionGeneratorInstance;

        public BaseReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance) {
            this.reflectionGeneratorInstance = reflectionGeneratorInstance;
        }

        public virtual BindingFlags GetBindingFlags() {
            return reflectionGeneratorInstance.defaultFlags;
        }

        public virtual string GetName(string defaultName) {
            return defaultName;
        }

        public virtual bool FieldAccessor() {
            return false;
        }
    }

    public class ReflectionGeneratorInstanceSetting : BaseReflectionGeneratorInstanceSetting {
        public ReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance)
            : base(reflectionGeneratorInstance) {}

        public BindingFlags? BindingFlags { get; set; }
        public string Name { get; set; }
        public bool IsField { get; set; }

        public override BindingFlags GetBindingFlags() {
            return BindingFlags ?? base.GetBindingFlags();
        }

        public override string GetName(string defaultName) {
            return Name ?? base.GetName(defaultName);
        }

        public override bool FieldAccessor() {
            return IsField;
        }
    }

    public class NullReflectionGeneratorInstanceSetting : BaseReflectionGeneratorInstanceSetting {
        public NullReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance)
            : base(reflectionGeneratorInstance) {}
    }
}