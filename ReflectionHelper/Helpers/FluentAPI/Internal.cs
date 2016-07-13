using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ReflectionFramework.Internal;

namespace ReflectionFramework.Internal {
    public class BaseReflectionGeneratorMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> where TReflectionGeneratorWrapper : ReflectionGeneratorWrapper<TWrapper> {
        protected readonly MemberInfo info;
        protected BindingFlags? flags;
        protected TReflectionGeneratorWrapper root;

        public BaseReflectionGeneratorMemberInfoInstance(MemberInfo info, TReflectionGeneratorWrapper root) {
            this.info = info;
            this.root = root;
        }
        protected void BindingFlagsImpl(BindingFlags flags) {
            root.WriteSetting(info, x => x.BindingFlags = flags);
        }
        protected void NameImpl(string name) {
            root.WriteSetting(info, x => x.Name = name);
        }
        protected void FallbackImpl(Delegate fallbackAction) {
            root.WriteSetting(info, x => x.FallbackAction = fallbackAction);
        }
    }

    public class ReflectionGeneratorMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> : BaseReflectionGeneratorMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> where TReflectionGeneratorWrapper : ReflectionGeneratorWrapper<TWrapper> {
        public ReflectionGeneratorMemberInfoInstance(MemberInfo info, TReflectionGeneratorWrapper root) : base(info, root) {}

        public TReflectionGeneratorWrapper EndMember() { return root; }
        public ReflectionGeneratorMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> BindingFlags(BindingFlags flags) { BindingFlagsImpl(flags); return this; }
        public ReflectionGeneratorMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> Name(string name) { NameImpl(name); return this; }
        public ReflectionGeneratorMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> Fallback(Delegate fallbackAction) { FallbackImpl(fallbackAction); return this; }        
    }

    public class ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> : BaseReflectionGeneratorMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> where TReflectionGeneratorWrapper : ReflectionGeneratorWrapper<TWrapper> {
        public ReflectionGeneratorPropertyMemberInfoInstance(MemberInfo info, TReflectionGeneratorWrapper root)
            : base(info, root) {}

        public TReflectionGeneratorWrapper EndMember() { return root; }
        public ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> BindingFlags(BindingFlags flags) { BindingFlagsImpl(flags); return this; }
        public ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> Name(string name) { NameImpl(name); return this; }
        public ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> Fallback(Delegate fallbackAction) { FallbackImpl(fallbackAction); return this; }

        public ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> FieldAccessor() {
            root.WriteSetting(info, x => x.IsField = true);
            return this;
        }

        public ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> GetterFallback(
            Delegate fallbackAction) {
            root.WriteSetting(info, x => x.GetterFallbackAction = fallbackAction);
            return this;
        }

        public ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> SetterFallback(
            Delegate fallbackAction) {
            root.WriteSetting(info, x => x.SetterFallbackAction = fallbackAction);
            return this;
        }
    }
}
