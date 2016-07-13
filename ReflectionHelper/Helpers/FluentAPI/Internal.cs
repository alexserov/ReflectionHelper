using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ReflectionFramework.Internal;

namespace ReflectionFramework.Internal {
    public class ReflectionGeneratorMemberInfoInstance<TWrapper> {
        internal readonly MemberInfo info;
        internal BindingFlags? flags;
        internal ReflectionGeneratorWrapper<TWrapper> root;

        public ReflectionGeneratorMemberInfoInstance(MemberInfo info, ReflectionGeneratorWrapper<TWrapper> root) {
            this.info = info;
            this.root = root;
        }
    }
    public class ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> :
        ReflectionGeneratorMemberInfoInstance<TWrapper> {
        public ReflectionGeneratorPropertyMemberInfoInstance(MemberInfo info, ReflectionGeneratorWrapper<TWrapper> root)
            : base(info, root) { }
    }
}
