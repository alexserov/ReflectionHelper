using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DevExpress.Xpf.Core.Internal {
    public class ReflectionGeneratedObject {
        public static readonly MethodInfo GetDelegateMethodInfo;
        public static readonly MethodInfo GetGenericDelegateMethodInfo;
        public static readonly MethodInfo GetFieldGetterMethodInfo;
        public static readonly MethodInfo GetFieldSetterMethodInfo;
        private readonly Dictionary<MethodInfo, Delegate> cache;

        static ReflectionGeneratedObject() {
            GetDelegateMethodInfo = GetMethodInfo(x => x.GetDelegate(null, null, null, false));
            GetGenericDelegateMethodInfo = GetMethodInfo(x => x.GetGenericDelegate(null, null, null, false, null));
            GetFieldGetterMethodInfo = GetMethodInfo(x => x.GetFieldGetter(null, null, null, null));
            GetFieldSetterMethodInfo = GetMethodInfo(x => x.GetFieldSetter(null, null, null, null));
        }

        static MethodInfo GetMethodInfo(Expression<Action<ReflectionGeneratedObject>> expr) {
            return (expr.Body as MethodCallExpression).Method;
        }
        public ReflectionGeneratedObject() {
            cache = new Dictionary<MethodInfo, Delegate>();
        }

        public Delegate GetFieldGetter(FieldInfo info, Type delegateType, Type tElement, Type tField) {
            return ReflectionHelper.CreateFieldGetterOrSetter(true,
                delegateType, info, tElement, tField);
        }
        public Delegate GetFieldSetter(FieldInfo info, Type delegateType, Type tElement, Type tField) {
            return ReflectionHelper.CreateFieldGetterOrSetter(false,
                delegateType, info, tElement, tField);
        }
        public Delegate GetGenericDelegate(MethodInfo info, Type instanceType, Type delegateType, bool useTyple,
            Type[] paramTypes) {
            return CreateDelegate(info.MakeGenericMethod(paramTypes), instanceType, delegateType, useTyple);
        }

        public Delegate GetDelegate(MethodInfo info, Type instanceType, Type delegateType, bool useTuple) {
            Delegate result;
            if (!cache.TryGetValue(info, out result)) {
                result = CreateDelegate(info, instanceType, delegateType, useTuple);
                cache[info] = result;
            }
            return result;
        }

        Delegate CreateDelegate(MethodInfo info, Type instanceType, Type delegateType, bool useTuple) {
            return (Delegate) ReflectionHelper.CreateMethodHandlerImpl(info, instanceType, delegateType, true, useTuple);
        }
    }
}