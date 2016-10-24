﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ReflectionFramework.Internal;

namespace ReflectionFramework.Internal {
    public class BaseInterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> where TInterfaceWrapper : ReflectionHelperInterfaceWrapperGenerator<TWrapper> {
        protected readonly MemberInfo info;
        protected BindingFlags? flags;
        protected TInterfaceWrapper root;

        public BaseInterfaceWrapperMemberInfoInstance(MemberInfo info, TInterfaceWrapper root) {
            this.info = info;
            this.root = root;
        }
        protected void BindingFlagsImpl(BindingFlags flags) {
            root.WriteSetting(info, x => x.BindingFlags = flags);
        }
        protected void NameImpl(string name) {
            root.WriteSetting(info, x => x.Name = name);
        }

        protected void InterfaceImpl(string name) {
            root.WriteSetting(info, x=>x.InterfaceName = name);            
            BindingFlagsImpl(BindingFlags.Instance | BindingFlags.NonPublic);
        }
        protected void InterfaceImpl(Type type) {
            InterfaceImpl(type.FullName);
        }
        protected void InterfaceImpl<T>() {
            InterfaceImpl(typeof(T));
        }
        protected void FallbackImpl(Delegate fallbackAction) {
            root.WriteSetting(info, x => x.FallbackAction = fallbackAction);
        }
    }

    public class InterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> : BaseInterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> where TInterfaceWrapper : ReflectionHelperInterfaceWrapperGenerator<TWrapper> {
        public InterfaceWrapperMemberInfoInstance(MemberInfo info, TInterfaceWrapper root) : base(info, root) {}

        public TInterfaceWrapper EndMember() { return root; }
        public InterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> BindingFlags(BindingFlags flags) { BindingFlagsImpl(flags); return this; }
        public InterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> Name(string name) { NameImpl(name); return this; }
        public InterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> Fallback(Delegate fallbackAction) { FallbackImpl(fallbackAction); return this; }
        public InterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> InterfaceMember(string name) { InterfaceImpl(name); return this; }
        public InterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> InterfaceMember<T>() { InterfaceImpl<T>(); return this; }
        public InterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> InterfaceMember(Type interfaceType) { InterfaceImpl(interfaceType); return this; }
    }

    public class InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> : BaseInterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> where TInterfaceWrapper : ReflectionHelperInterfaceWrapperGenerator<TWrapper> {
        public InterfaceWrapperPropertyMemberInfoInstance(MemberInfo info, TInterfaceWrapper root)
            : base(info, root) {}

        public TInterfaceWrapper EndMember() { return root; }
        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> BindingFlags(BindingFlags flags) { BindingFlagsImpl(flags); return this; }
        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> Name(string name) { NameImpl(name); return this; }
        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> Fallback(Delegate fallbackAction) { FallbackImpl(fallbackAction); return this; }
        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> InterfaceMember(string name) { InterfaceImpl(name); return this; }
        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> InterfaceMember<T>() { InterfaceImpl<T>(); return this; }
        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> InterfaceMember(Type interfaceType) { InterfaceImpl(interfaceType); return this; }

        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> FieldAccessor() {
            root.WriteSetting(info, x => x.IsField = true);
            return this;
        }

        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> GetterFallback(
            Delegate fallbackAction) {
            root.WriteSetting(info, x => x.GetterFallbackAction = fallbackAction);
            return this;
        }

        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> SetterFallback(
            Delegate fallbackAction) {
            root.WriteSetting(info, x => x.SetterFallbackAction = fallbackAction);
            return this;
        }
    }
}
