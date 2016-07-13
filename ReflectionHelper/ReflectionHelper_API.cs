using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ReflectionFramework {
    public partial class ReflectionHelper {
        public static Action<object, Delegate, object, Delegate> CreatePushValueMethod(MethodInfo setValueDelegate,
            MethodInfo getValueDelegate) {
            var dm = new DynamicMethod(string.Empty, null,
                new[] {typeof(object), typeof(Delegate), typeof(object), typeof(Delegate)});
            var ig = dm.GetILGenerator();
            ig.Emit(OpCodes.Ldarg, (short) 1);
            ig.Emit(OpCodes.Castclass, setValueDelegate.DeclaringType);
            ig.Emit(OpCodes.Ldarg, (short) 0);
            ig.Emit(OpCodes.Ldarg, (short) 3);
            ig.Emit(OpCodes.Castclass, getValueDelegate.DeclaringType);
            ig.Emit(OpCodes.Ldarg, (short) 2);
            ig.Emit(OpCodes.Callvirt, getValueDelegate);
            ig.Emit(OpCodes.Callvirt, setValueDelegate);
            ig.Emit(OpCodes.Ret);
            return
                (Action<object, Delegate, object, Delegate>)
                    dm.CreateDelegate(typeof(Action<object, Delegate, object, Delegate>));
        }

        public static Func<TElement, TField> CreateFieldGetter<TElement, TField>(Type declaringType, string fieldName,
            BindingFlags bFlags = BindingFlags.Instance | BindingFlags.Public) {
            return
                (Func<TElement, TField>)
                    CreateFieldGetterOrSetter<TElement, TField>(true, typeof(Func<TElement, TField>), declaringType,
                        fieldName, bFlags);
        }

        public static Action<TElement, TField> CreateFieldSetter<TElement, TField>(Type declaringType, string fieldName,
            BindingFlags bFlags = BindingFlags.Instance | BindingFlags.Public) {
            return
                (Action<TElement, TField>)
                    CreateFieldGetterOrSetter<TElement, TField>(false, typeof(Action<TElement, TField>), declaringType,
                        fieldName, bFlags);
        }

        public static Delegate CreateInstanceMethodHandler(object instance, string methodName, BindingFlags bindingFlags,
            Type instanceType, int? parametersCount = null, Type[] typeParameters = null, bool callVirtIfNeeded = true) {
            return
                (Delegate)
                    CreateMethodHandlerImpl(instance, methodName, bindingFlags, instanceType, null, parametersCount,
                        typeParameters, callVirtIfNeeded);
        }

        public static TDelegate CreateInstanceMethodHandler<TInstance, TDelegate>(TInstance entity, string methodName,
            BindingFlags bindingFlags, int? parametersCount = null, Type[] typeParameters = null,
            bool callVirtIfNeeded = true) {
            return
                (TDelegate)
                    CreateMethodHandlerImpl(entity, methodName, bindingFlags, typeof(TInstance), typeof(TDelegate),
                        parametersCount, typeParameters, callVirtIfNeeded);
        }

        public static TDelegate CreateInstanceMethodHandler<TDelegate>(object instance, string methodName,
            BindingFlags bindingFlags, Type instanceType, int? parametersCount = null, Type[] typeParameters = null,
            bool callVirtIfNeeded = true) {
            return
                (TDelegate)
                    CreateMethodHandlerImpl(instance, methodName, bindingFlags, instanceType, typeof(TDelegate),
                        parametersCount, null, callVirtIfNeeded);
        }

        public T GetStaticMethodHandler<T>(Type entityType, string methodName, BindingFlags bindingFlags)
            where T : class {
            object method;
            var key = new HelperKey(entityType, methodName, typeof(T), null, null, true);
            if (!InvokeInfo.TryGetValue(key, out method)) {
                method = CreateMethodHandlerImpl(null, methodName, bindingFlags, entityType, typeof(T), null, null, true);
                InvokeInfo[key] = method;
            }
            return (T) method;
        }

        public T GetInstanceMethodHandler<T>(object entity, string methodName, BindingFlags bindingFlags,
            Type instanceType, int? parametersCount = null, Type[] typeParameters = null, bool callVirtIfNeeded = true) {
            object method;
            var key = new HelperKey(instanceType, methodName, typeof(T), parametersCount, typeParameters,
                callVirtIfNeeded);
            if (!InvokeInfo.TryGetValue(key, out method)) {
                method = CreateInstanceMethodHandler<T>(entity, methodName, bindingFlags, instanceType, parametersCount,
                    typeParameters, callVirtIfNeeded);
                InvokeInfo[key] = method;
            }
            return (T) method;
        }

        public T GetPropertyValue<T>(object entity, string propertyName,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) {
            return (T) GetPropertyValue(entity, propertyName, bindingFlags);
        }

        public object GetPropertyValue(object entity, string propertyName,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) {
            object getter;
            var type = entity.GetType();
            var key = new HelperKey(type, propertyName, typeof(Func<object, object>), null, null, true);
            if (!InvokeInfo.TryGetValue(key, out getter)) {
                var pi = type.GetProperty(propertyName, bindingFlags);
                getter = CreateGetter(pi);
                InvokeInfo[key] = getter;
            }
            var func = (Func<object, object>) getter;
            return func(entity);
        }

        public void SetPropertyValue(object entity, string propertyName, object value,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) {
            object setter;
            var type = entity.GetType();
            var key = new HelperKey(type, propertyName, typeof(Action<object, object>), null, null, true);
            if (!InvokeInfo.TryGetValue(key, out setter)) {
                var pi = type.GetProperty(propertyName, bindingFlags);
                setter = CreateSetter(pi);
                InvokeInfo[key] = setter;
            }
            var del = (Action<object, object>) setter;
            del(entity, value);
        }

        public Type GetPropertyType(object entity, string propertyName,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) {
            Type propertyType;
            var type = entity.GetType();
            var key = new HelperKey(type, propertyName, null, null, null, true);
            if (!PropertyTypeInfo.TryGetValue(key, out propertyType)) {
                var pi = type.GetProperty(propertyName, bindingFlags);
                propertyType = pi.PropertyType;
            }
            return propertyType;
        }
    }
}