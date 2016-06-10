using System.Reflection;

namespace DevExpress.Xpf.Core.Internal {
    public class ReflectionGeneratorMemberInfoInstance<TWrapper> where TWrapper : class {
        private readonly MemberInfo info;
        ReflectionGeneratorInstance<TWrapper> root;
        private BindingFlags? flags;

        public ReflectionGeneratorMemberInfoInstance(MemberInfo info, ReflectionGeneratorInstance<TWrapper> root) {
            this.info = info;
            this.root = root;
        }

        public ReflectionGeneratorInstance<TWrapper> EndMember() {
            return root;
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper> BindingFlags(BindingFlags flags) {
            root.WriteSetting(info, x => x.BindingFlags = flags);
            return this;
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper> FieldAccessor() {
            return this;
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper> Name(string name) {
            root.WriteSetting(info, x => x.Name = name);
            return this;
        }
    }
}