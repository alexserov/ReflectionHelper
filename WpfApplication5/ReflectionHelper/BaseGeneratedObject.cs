using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DevExpress.Xpf.Core.Internal {
    public class ReflectionGeneratedObject {
        public static readonly MethodInfo GetDelegateMethodInfo;
        public static readonly MethodInfo GetGenericDelegateMethodInfo;        
        private readonly Dictionary<MethodInfo, Delegate> cache;

        static ReflectionGeneratedObject() {
            GetDelegateMethodInfo = GetMethodInfo(x => x.GetDelegate(null, null, null, false));
            GetGenericDelegateMethodInfo = GetMethodInfo(x => x.GetGenericDelegate(null, null, null, false, null));            
        }

        static MethodInfo GetMethodInfo(Expression<Action<ReflectionGeneratedObject>> expr) {
            return (expr.Body as MethodCallExpression).Method;
        }
        public ReflectionGeneratedObject() {
            cache = new Dictionary<MethodInfo, Delegate>();
        }

        public Delegate GetGenericDelegate(MethodInfo info, Type instanceType, Type delegateType, bool useTyple, Type[] paramTypes) {
            return CreateDelegate(info.MakeGenericMethod(paramTypes), instanceType, delegateType, useTyple);
            //return GetGenericDelegate(info, instanceType, delegateType, useTyple, t1, null);
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