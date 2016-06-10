using System;
using System.Reflection;

namespace DevExpress.Xpf.Core.Internal {
    public class ReflectionGeneratorMemberInfoInstance<TWrapper> {
        internal readonly MemberInfo info;
        internal BindingFlags? flags;
        internal ReflectionGeneratorInstance<TWrapper> root;

        public ReflectionGeneratorMemberInfoInstance(MemberInfo info, ReflectionGeneratorInstance<TWrapper> root) {
            this.info = info;
            this.root = root;
        }
    }

    public static class ReflectionGeneratorMemberInfoInstanceExtensions {
        public static ReflectionGeneratorInstance<TWrapper> EndMember<TWrapper>(
            this ReflectionGeneratorMemberInfoInstance<TWrapper> instance) {
            return instance.root;
        }

        public static ReflectionGeneratorMemberInfoInstance<TWrapper> BindingFlags<TWrapper>(
            this ReflectionGeneratorMemberInfoInstance<TWrapper> instance, BindingFlags flags) {
            instance.root.WriteSetting(instance.info, x => x.BindingFlags = flags);
            return instance;
        }

        public static ReflectionGeneratorMemberInfoInstance<TWrapper> Name<TWrapper>(
            this ReflectionGeneratorMemberInfoInstance<TWrapper> instance, string name) {
            instance.root.WriteSetting(instance.info, x => x.Name = name);
            return instance;
        }
        public static ReflectionGeneratorMemberInfoInstance<TWrapper> Fallback<TWrapper>(
            this ReflectionGeneratorMemberInfoInstance<TWrapper> instance, Delegate fallbackAction) {
            instance.root.WriteSetting(instance.info, x => x.FallbackAction = fallbackAction);
            return instance;
        }
    }

    public static class ReflectionGeneratorPropertyMemberInfoInstanceExtensions {
        public static ReflectionGeneratorInstance<TWrapper> EndMember<TWrapper>(
            this ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> instance) {
            return ReflectionGeneratorMemberInfoInstanceExtensions.EndMember(instance);
        }

        public static ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> BindingFlags<TWrapper>(
            this ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> instance, BindingFlags flags) {
            return
                (ReflectionGeneratorPropertyMemberInfoInstance<TWrapper>)
                    ReflectionGeneratorMemberInfoInstanceExtensions.BindingFlags(instance, flags);
        }

        public static ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> Name<TWrapper>(
            this ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> instance, string name) {
            return
                (ReflectionGeneratorPropertyMemberInfoInstance<TWrapper>)
                    ReflectionGeneratorMemberInfoInstanceExtensions.Name(instance, name);
        }
        public static ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> Fallback<TWrapper>(
            this ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> instance, Delegate fallbackAction) {
            return
                (ReflectionGeneratorPropertyMemberInfoInstance<TWrapper>)
                    ReflectionGeneratorMemberInfoInstanceExtensions.Fallback(instance, fallbackAction);
        }

        public static ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> FieldAccessor<TWrapper>(
            this ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> instance) {
            instance.root.WriteSetting(instance.info, x => x.IsField = true);
            return instance;
        }
        public static ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> GetterFallback<TWrapper>(
            this ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> instance, Delegate fallbackAction) {
            instance.root.WriteSetting(instance.info, x => x.GetterFallbackAction = fallbackAction);
            return instance;
        }
        public static ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> SetterFallback<TWrapper>(
            this ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> instance, Delegate fallbackAction) {
            instance.root.WriteSetting(instance.info, x => x.SetterFallbackAction = fallbackAction);
            return instance;
        }
    }

    public class ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> :
        ReflectionGeneratorMemberInfoInstance<TWrapper> {
        public ReflectionGeneratorPropertyMemberInfoInstance(MemberInfo info, ReflectionGeneratorInstance<TWrapper> root)
            : base(info, root) {}
    }
}