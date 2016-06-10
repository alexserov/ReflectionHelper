using System;
using System.Collections.Generic;
using System.Reflection;

namespace DevExpress.Xpf.Core.Internal {
    public class ReflectionGeneratedObject {
        public static readonly MethodInfo GetDelegateMethodInfo;
        private readonly Dictionary<MethodInfo, Delegate> cache;

        static ReflectionGeneratedObject() {
            GetDelegateMethodInfo = typeof(ReflectionGeneratedObject).GetMethod("GetDelegate");
        }

        public ReflectionGeneratedObject() {
            cache = new Dictionary<MethodInfo, Delegate>();
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