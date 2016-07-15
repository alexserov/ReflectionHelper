using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ReflectionHelper;

namespace ReflectionFramework.Internal {
    public class ReflectionGeneratedObject {
        const int CallsToCleanup = 100;
        class WeakReference<T> where T : class {
            WeakReference impl;
            public bool IsAlive { get { return impl.IsAlive; } }
            public T Target { get { return (T)impl.Target; } }
            public bool TrackResurrection { get { return impl.TrackResurrection; } }

            public WeakReference(object obj, bool trackResurrection) { impl = new WeakReference(obj, trackResurrection); }
            public WeakReference(object obj) : this(obj, false) {}
            public WeakReference() : this(null) {}
        }
        sealed class GlobalCacheKey<TLocalKey> where TLocalKey : struct {
            bool Equals(GlobalCacheKey<TLocalKey> other) {
                return Key.Equals(other.Key);
            }
            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is GlobalCacheKey<TLocalKey> && Equals((GlobalCacheKey<TLocalKey>)obj);
            }

            public override int GetHashCode() {
                return Key.GetHashCode();
            }

            public GlobalCacheKey(TLocalKey value) {
                Key = value;
            }
            TLocalKey Key { get; }         
        }

        struct LocalFieldCacheKey {
            bool Equals(LocalFieldCacheKey other) {
                return addThisArgForStatic == other.addThisArgForStatic && delegateType == other.delegateType && Equals(info, other.info) && tElement == other.tElement && tField == other.tField;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                return obj is LocalFieldCacheKey && Equals((LocalFieldCacheKey)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    var hashCode = addThisArgForStatic.GetHashCode();
                    hashCode = (hashCode*397) ^ (delegateType != null ? delegateType.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (info != null ? info.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (tElement != null ? tElement.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (tField != null ? tField.GetHashCode() : 0);
                    return hashCode;
                }
            }

            readonly bool addThisArgForStatic;
            readonly Type delegateType;
            readonly FieldInfo info;
            readonly Type tElement;
            readonly Type tField;

            public LocalFieldCacheKey(FieldInfo info, Type delegateType, Type tElement, Type tField, bool addThisArgForStatic) {
                this.info = info;
                this.delegateType = delegateType;
                this.tElement = tElement;
                this.tField = tField;
                this.addThisArgForStatic = addThisArgForStatic;
            }
        }

        struct LocalGenericDelegateCacheKey {
            bool Equals(LocalGenericDelegateCacheKey other) {
                return delegateType == other.delegateType && Equals(info, other.info) && instanceType == other.instanceType && Equals(paramTypes, other.paramTypes) && useTyple == other.useTyple;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                return obj is LocalGenericDelegateCacheKey && Equals((LocalGenericDelegateCacheKey)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    var hashCode = (delegateType != null ? delegateType.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (info != null ? info.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (instanceType != null ? instanceType.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (paramTypes != null ? paramTypes.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ useTyple.GetHashCode();
                    return hashCode;
                }
            }

            readonly Type delegateType;
            readonly MethodInfo info;
            readonly Type instanceType;
            readonly Type[] paramTypes;
            readonly bool useTyple;

            public LocalGenericDelegateCacheKey(MethodInfo info, Type instanceType, Type delegateType, bool useTyple, Type[] paramTypes) {
                this.info = info;
                this.instanceType = instanceType;
                this.delegateType = delegateType;
                this.useTyple = useTyple;
                this.paramTypes = paramTypes;
            }
        }

        struct LocalDelegateCacheKey {
            bool Equals(LocalDelegateCacheKey other) {
                return delegateType == other.delegateType && Equals(info, other.info) && instanceType == other.instanceType && useTuple == other.useTuple;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                return obj is LocalDelegateCacheKey && Equals((LocalDelegateCacheKey)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    var hashCode = (delegateType != null ? delegateType.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (info != null ? info.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (instanceType != null ? instanceType.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ useTuple.GetHashCode();
                    return hashCode;
                }
            }

            readonly Type delegateType;
            readonly MethodInfo info;
            readonly Type instanceType;
            readonly bool useTuple;

            public LocalDelegateCacheKey(MethodInfo info, Type instanceType, Type delegateType, bool useTuple) {
                this.info = info;
                this.instanceType = instanceType;
                this.delegateType = delegateType;
                this.useTuple = useTuple;
            }
        }
        public static readonly MethodInfo GetDelegateMethodInfo;
        public static readonly MethodInfo GetGenericDelegateMethodInfo;
        public static readonly MethodInfo GetFieldGetterMethodInfo;
        public static readonly MethodInfo GetFieldSetterMethodInfo;
        public static readonly MethodInfo WrapMethodInfo;
        public static readonly MethodInfo UnwrapMethodInfo;
        static readonly Dictionary<LocalFieldCacheKey, WeakReference<Delegate>> globalFieldGetterCache;
        static readonly Dictionary<LocalFieldCacheKey, WeakReference<Delegate>> globalFieldSetterCache;
        static readonly Dictionary<LocalGenericDelegateCacheKey, WeakReference<Delegate>> globalGenericDelegateCache;
        static readonly Dictionary<LocalDelegateCacheKey, WeakReference<Delegate>> globalDelegateCache;

        static ReflectionGeneratedObject() {
            globalFieldGetterCache = new Dictionary<LocalFieldCacheKey, WeakReference<Delegate>>();
            globalFieldSetterCache = new Dictionary<LocalFieldCacheKey, WeakReference<Delegate>>();
            globalGenericDelegateCache = new Dictionary<LocalGenericDelegateCacheKey, WeakReference<Delegate>>();
            globalDelegateCache = new Dictionary<LocalDelegateCacheKey, WeakReference<Delegate>>();
            GetDelegateMethodInfo = GetMethodInfo(x => x.GetDelegate(null, null, null, false));
            GetGenericDelegateMethodInfo = GetMethodInfo(x => x.GetGenericDelegate(null, null, null, false, null));
            GetFieldGetterMethodInfo = GetMethodInfo(x => x.GetFieldGetter(null, null, null, null, false));
            GetFieldSetterMethodInfo = GetMethodInfo(x => x.GetFieldSetter(null, null, null, null, false));
            WrapMethodInfo = GetMethodInfo(x => x.Wrap(null, null));
            UnwrapMethodInfo = GetMethodInfo(x => x.Unwrap(null));
        }

        readonly Dictionary<LocalFieldCacheKey, Delegate> localFieldGetterCache;
        readonly Dictionary<LocalFieldCacheKey, Delegate> localFieldSetterCache;
        readonly Dictionary<LocalGenericDelegateCacheKey, Delegate> localGenericDelegateCache;
        readonly Dictionary<LocalDelegateCacheKey, Delegate> localDelegateCache;

        object source;

        public ReflectionGeneratedObject() {
            localFieldGetterCache = new Dictionary<LocalFieldCacheKey, Delegate>();
            localFieldSetterCache = new Dictionary<LocalFieldCacheKey, Delegate>();
            localGenericDelegateCache = new Dictionary<LocalGenericDelegateCacheKey, Delegate>();
            localDelegateCache = new Dictionary<LocalDelegateCacheKey, Delegate>();
        }

        public ReflectionGeneratedObject(object source) : this() {
            this.source = source;
        }

        static MethodInfo GetMethodInfo(Expression<Action<ReflectionGeneratedObject>> expr) {
            return (expr.Body as MethodCallExpression).Method;
        }

        static int currentCalls;
        bool GetCachedDelegate<TKey>(TKey key, Dictionary<TKey, WeakReference<Delegate>> globalCache, Dictionary<TKey, Delegate> localCache, out Delegate result) where TKey : struct {
            if (localCache.TryGetValue(key, out result)) {
                return true;
            }
            WeakReference<Delegate> globalResult;
            if (globalCache.TryGetValue(key, out globalResult)) {
                result = globalResult.Target;
                if (!globalResult.IsAlive) {
                    globalCache.Remove(key);
                    return false;
                }
                localCache[key] = result;
                CheckCleanup();
                return true;
            }
            return false;
        }

        void SetCachedDelegate<TKey>(TKey key, Delegate result, Dictionary<TKey, WeakReference<Delegate>> globalCache, Dictionary<TKey, Delegate> localCache) where TKey : struct {
            localCache[key] = result;
            globalCache[key] = new WeakReference<Delegate>(result);
            CheckCleanup();
        }

        static bool performingCleanup;
        static readonly object cleanupLock = new object();
        static void CheckCleanup() {
            currentCalls++;
            if (currentCalls > CallsToCleanup) {
                if(performingCleanup)
                    return;
                lock (cleanupLock) {
                    if(performingCleanup || currentCalls < CallsToCleanup)
                        return;
                    try {
                        performingCleanup = true;
                        DoLockedCleanup(globalDelegateCache);
                        DoLockedCleanup(globalFieldGetterCache);
                        DoLockedCleanup(globalFieldSetterCache);
                        DoLockedCleanup(globalGenericDelegateCache);
                    }
                    finally {
                        performingCleanup = false;
                    }
                }
            }
        }

        static void DoLockedCleanup<TKey>(Dictionary<TKey, WeakReference<Delegate>> dictionary) where TKey : struct {
            foreach (var key in dictionary.Keys.ToArray()) {
                if (!dictionary[key].IsAlive) {
                    dictionary.Remove(key);
                }
            }
        }

        public Delegate GetFieldGetter(FieldInfo info, Type delegateType, Type tElement, Type tField,
            bool addThisArgForStatic) {
            Delegate result;
            var key = new LocalFieldCacheKey(info, delegateType, tElement, tField, addThisArgForStatic);
            if (!GetCachedDelegate(key, globalFieldGetterCache, localFieldGetterCache, out result)) {
                result = ReflectionHelper.CreateFieldGetterOrSetter(true, delegateType, info, tElement, tField, !addThisArgForStatic);
                SetCachedDelegate(key, result, globalFieldGetterCache, localFieldGetterCache);
            }
            return result;
        }

        public Delegate GetFieldSetter(FieldInfo info, Type delegateType, Type tElement, Type tField,
            bool addThisArgForStatic) {
            Delegate result;
            var key = new LocalFieldCacheKey(info, delegateType, tElement, tField, addThisArgForStatic);
            if (!GetCachedDelegate(key, globalFieldSetterCache, localFieldSetterCache, out result)) {
                result = ReflectionHelper.CreateFieldGetterOrSetter(false, delegateType, info, tElement, tField, !addThisArgForStatic);
                SetCachedDelegate(key, result, globalFieldSetterCache, localFieldSetterCache);
            }
            return result;            
        }

        public Delegate GetGenericDelegate(MethodInfo info, Type instanceType, Type delegateType, bool useTyple,
            Type[] paramTypes) {
            Delegate result;
            var key = new LocalGenericDelegateCacheKey(info, instanceType, delegateType, useTyple, paramTypes);
            if (!GetCachedDelegate(key, globalGenericDelegateCache, localGenericDelegateCache, out result)) {
                result = CreateDelegate(info.MakeGenericMethod(paramTypes), instanceType, delegateType, useTyple);
                SetCachedDelegate(key, result, globalGenericDelegateCache, localGenericDelegateCache);
            }
            return result;
        }

        public Delegate GetDelegate(MethodInfo info, Type instanceType, Type delegateType, bool useTuple) {
            Delegate result;
            var key = new LocalDelegateCacheKey(info, instanceType, delegateType, useTuple);
            if (!GetCachedDelegate(key, globalDelegateCache, localDelegateCache, out result)) {
                result = CreateDelegate(info, instanceType, delegateType, useTuple);
                SetCachedDelegate(key, result, globalDelegateCache, localDelegateCache);
            }
            return result;
        }

        Delegate CreateDelegate(MethodInfo info, Type instanceType, Type delegateType, bool useTuple) {
            return (Delegate) ReflectionHelper.CreateMethodHandlerImpl(info, instanceType, delegateType, true, useTuple);
        }

        static void DoNotRemove(object obj) {
            
        }
        public object Wrap(Type wrapperType, object obj) {
            DoNotRemove(obj);
            DoNotRemove(wrapperType);
            //Log.Write($"Wrap for {wrapperType}; object is {obj}");
            return ReflectionGenerator.Wrap(obj, wrapperType);
        }
        public object Unwrap(ReflectionGeneratedObject wrapper) {
            DoNotRemove(wrapper);
            return wrapper.source;
        }
    }
}