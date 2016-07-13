using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ReflectionFramework {
    public partial class ReflectionHelper {
        public ReflectionHelper() {
            InvokeInfo = new Dictionary<HelperKey, object>();
            PropertyTypeInfo = new Dictionary<HelperKey, Type>();
        }

        Dictionary<HelperKey, object> InvokeInfo { get; }
        Dictionary<HelperKey, Type> PropertyTypeInfo { get; }

        public bool HasContent {
            get { return InvokeInfo.Count > 0; }
        }

        Func<object, object> CreateGetter(PropertyInfo info) {
            return
                (Func<object, object>)
                    CreateMethodHandlerImpl(info.GetGetMethod(true), null, typeof(Func<object, object>), true);
        }

        Action<object, object> CreateSetter(PropertyInfo info) {
            if (!info.CanWrite)
                throw new NotSupportedException("no setter");
            return
                (Action<object, object>)
                    CreateMethodHandlerImpl(info.GetSetMethod(true), null, typeof(Action<object, object>), true);
        }

        static object CreateMethodHandlerImpl(object instance, string methodName, BindingFlags bindingFlags,
            Type instanceType, Type delegateType, int? parametersCount, Type[] typeParameters, bool callVirtIfNeeded) {
            MethodInfo mi = null;
            if (instance != null)
                mi = GetMethod(instance.GetType(), methodName, bindingFlags, parametersCount, typeParameters);
            mi = mi ?? GetMethod(instanceType, methodName, bindingFlags, parametersCount, typeParameters);
            return CreateMethodHandlerImpl(mi, instanceType, delegateType, callVirtIfNeeded);
        }

        static MethodInfo GetMethod(Type type, string methodName, BindingFlags bindingFlags,
            int? parametersCount = null,
            Type[] typeParameters = null) {
            if (parametersCount != null) {
                return
                    type.GetMethods(bindingFlags)
                        .Where(x => x.Name == methodName)
                        .First(x => x.GetParameters().Count() == parametersCount.Value);
            }
            if (typeParameters != null) {
                return type.GetMethods(bindingFlags).Where(x => x.Name == methodName).First(x => {
                    var i = 0;
                    foreach (var param in x.GetParameters()) {
                        if (!typeParameters[i].IsAssignableFrom(param.ParameterType))
                            return false;
                        i++;
                    }
                    return true;
                });
            }
            return type.GetMethod(methodName, bindingFlags);
        }


        internal static void CastClass(ILGenerator generator, Type sourceType, Type targetType) {
            if (Equals(null, targetType))
                return;
            if (sourceType == targetType)
                return;
            var oneIsVoid = typeof(void) == sourceType || typeof(void) == targetType;
            var sourceIsNull = Equals(null, sourceType);
            if (oneIsVoid && !sourceIsNull)
                throw new InvalidOperationException(string.Format("Cast from {0} to {1} is not supported", sourceType,
                    targetType));
            if (Equals(null, sourceType)) {
                if (targetType.IsClass)
                    generator.Emit(OpCodes.Castclass, targetType);
                else
                    generator.Emit(OpCodes.Unbox_Any, targetType);
            }
            //box
            if (sourceType.IsValueType && !targetType.IsValueType) {
                generator.Emit(OpCodes.Box, sourceType);
                generator.Emit(OpCodes.Castclass, targetType);
            }
            //unbox
            if (!sourceType.IsValueType && targetType.IsValueType)
                generator.Emit(OpCodes.Unbox_Any, targetType);
            //cast
            if (Equals(sourceType.IsValueType, targetType.IsValueType) && !(sourceType == targetType))
                generator.Emit(OpCodes.Castclass, targetType);
        }

        internal static object CreateMethodHandlerImpl(MethodInfo mi, Type instanceType, Type delegateType,
            bool callVirtIfNeeded, bool? useTuple2 = null) {
            var isStatic = mi.IsStatic;

            var thisArgType = instanceType ?? mi.DeclaringType;
            var returnType = mi.ReturnType;
            var useTuple = false;
            Type[] delegateGenericArguments;
            var skipArgumentLengthCheck = false;
            var sourceParametersTypes = mi.GetParameters().Select(x => x.ParameterType).ToArray();
            if (delegateType == null) {
                delegateType = MakeGenericDelegate(sourceParametersTypes, ref returnType, isStatic ? null : thisArgType,
                    out useTuple);
                delegateGenericArguments = sourceParametersTypes;
                skipArgumentLengthCheck = true;
            }
            else {
                var invokeMethod = delegateType.GetMethod("Invoke");
                delegateGenericArguments = invokeMethod.GetParameters().Select(x => x.ParameterType).ToArray();
                if (!isStatic)
                    thisArgType = delegateGenericArguments[0];
                returnType = invokeMethod.ReturnType;
            }
            useTuple = useTuple2 ?? useTuple;
            if (!skipArgumentLengthCheck &&
                delegateGenericArguments.Length !=
                (isStatic ? sourceParametersTypes.Count() : sourceParametersTypes.Count() + 1))
                throw new ArgumentException("Invalid delegate arguments count");

            var resultParametersTypes = delegateGenericArguments.Skip(isStatic ? 0 : 1);
            var dynamicMethodParameterTypes =
                (isStatic ? resultParametersTypes : new[] {thisArgType}.Concat(resultParametersTypes)).ToArray();

            DynamicMethod dm;
            if (mi.IsVirtual && !callVirtIfNeeded)
                dm = new DynamicMethod(string.Empty, returnType, dynamicMethodParameterTypes, mi.DeclaringType, true);
            else
                dm = new DynamicMethod(string.Empty, returnType, dynamicMethodParameterTypes, true);
            var ig = dm.GetILGenerator();

            byte newLocalIndex = 0;
            var localBuilders = new List<LocalBuilder>();
            if (!isStatic) {
                localBuilders.Add(ig.DeclareLocal(mi.DeclaringType));
                newLocalIndex = 1;
            }
            foreach (var type in mi.GetParameters().Select(x => x.ParameterType)) {
                localBuilders.Add(ig.DeclareLocal(GetElementTypeIfNeeded(type)));
            }
            if (!isStatic) {
                var isValueType = mi.DeclaringType.IsValueType;
                ig.Emit(OpCodes.Ldarg_0);
                CastClass(ig, thisArgType, mi.DeclaringType);
                if (isValueType) {
                    ig.Emit(OpCodes.Stloc_0);
                    ig.Emit(OpCodes.Ldloca_S, 0);
                }
            }
            var argumentIndex = mi.IsStatic ? (short) 0 : (short) 1;

            for (var parameterIndex = 0; parameterIndex < sourceParametersTypes.Length; parameterIndex++) {
                var parameter = mi.GetParameters()[argumentIndex - newLocalIndex];
                if (!parameter.IsOut) {
                    ig.Emit(OpCodes.Ldarg, argumentIndex);
                    CastClass(ig, GetElementTypeIfNeeded(resultParametersTypes.ElementAt(parameterIndex)),
                        GetElementTypeIfNeeded(sourceParametersTypes[parameterIndex]));
                    ig.Emit(OpCodes.Stloc, localBuilders[argumentIndex]);
                    if (!parameter.ParameterType.IsByRef)
                        ig.Emit(OpCodes.Ldloc, localBuilders[argumentIndex]);
                    else
                        ig.Emit(OpCodes.Ldloca, localBuilders[argumentIndex]);
                }
                else {
                    ig.Emit(OpCodes.Ldloca, localBuilders[argumentIndex]);
                }
                argumentIndex++;
            }
            if (mi.IsVirtual && callVirtIfNeeded)
                ig.Emit(OpCodes.Callvirt, mi);
            else
                ig.Emit(OpCodes.Call, mi);

            //building tuple            
            if (useTuple) {
                if (mi.ReturnType != typeof(void)) {
                    CastClass(ig, mi.ReturnType, returnType.GetGenericArguments()[0]);
                }
                for (var parameterIndex = 0; parameterIndex < sourceParametersTypes.Length; parameterIndex++) {
                    if (sourceParametersTypes[parameterIndex].IsByRef) {
                        ig.Emit(OpCodes.Ldloc, localBuilders[newLocalIndex + parameterIndex]);
                        CastClass(ig, sourceParametersTypes[parameterIndex].GetElementType(),
                            resultParametersTypes.ElementAt(parameterIndex));
                    }
                }
                ig.Emit(OpCodes.Newobj, returnType.GetConstructors().First(x => x.GetParameters().Length > 0));
            }
            else {
                CastClass(ig, mi.ReturnType, returnType);
            }
            ig.Emit(OpCodes.Ret);
            return dm.CreateDelegate(delegateType);
        }

        static Type GetElementTypeIfNeeded(Type x) {
            if (x.IsByRef)
                return x.GetElementType();
            return x;
        }

        internal static Type MakeGenericDelegate(Type[] parameterTypes, ref Type returnType, Type thisArgType,
            out bool useTuple) {
            useTuple = false;
            Type resultType = null;
            var hasReturnType = returnType != null && returnType != typeof(void);
            var parametersCount = parameterTypes.Length;
            if (thisArgType != null)
                parametersCount += 1;
            var lst = new List<Type>();
            var tupleArgs = new List<Type>();
            if (thisArgType != null)
                lst.Add(thisArgType);
            foreach (var parameterType in parameterTypes) {
                if (parameterType.IsByRef) {
                    var resultParameterType = parameterType.GetElementType();
                    lst.Add(resultParameterType);
                    tupleArgs.Add(resultParameterType);
                }
                else {
                    lst.Add(parameterType);
                }
            }
            var hasByRefArgs = tupleArgs.Count > 0;
            if (hasReturnType && !hasByRefArgs) {
                lst.Add(returnType);
            }
            if (hasByRefArgs) {
                if (hasReturnType)
                    tupleArgs.Insert(0, returnType);
                returnType =
                    typeof(Tuple<>).Assembly.GetType(string.Format("System.Tuple`{0}", tupleArgs.Count))
                        .MakeGenericType(tupleArgs.ToArray());
                lst.Add(returnType);
                useTuple = true;
            }
            hasReturnType = hasReturnType || hasByRefArgs;
            switch (parametersCount) {
                case 0:
                    resultType = hasReturnType ? typeof(Func<>) : typeof(Action);
                    break;
                case 1:
                    resultType = hasReturnType ? typeof(Func<,>) : typeof(Action<>);
                    break;
                case 2:
                    resultType = hasReturnType ? typeof(Func<,,>) : typeof(Action<,>);
                    break;
                default:
                    resultType = hasReturnType
                        ? typeof(Func<>).Assembly.GetType(string.Format("System.Func`{0}", parametersCount + 1))
                        : typeof(Func<>).Assembly.GetType(string.Format("System.Action`{0}", parametersCount));
                    break;
            }
            if (lst.Count == 0)
                return resultType;
            return resultType.MakeGenericType(lst.ToArray());
        }

        static Delegate CreateFieldGetterOrSetter<TElement, TField>(bool isGetter, Type delegateType,
            Type declaringType,
            string fieldName, BindingFlags bFlags) {
            var fieldInfo = declaringType.GetField(fieldName, bFlags);
            return CreateFieldGetterOrSetter(isGetter, delegateType, fieldInfo, typeof(TElement), typeof(TField), true);
        }

        internal static Delegate CreateFieldGetterOrSetter(bool isGetter, Type delegateType, FieldInfo fieldInfo,
            Type tElement, Type tField, bool addThisArgForStatic) {
            var isStatic = fieldInfo.IsStatic;
            DynamicMethod dm;
            if (isGetter)
                dm = new DynamicMethod(string.Empty, tField,
                    !addThisArgForStatic && isStatic ? null : new[] {tElement}, true);
            else
                dm = new DynamicMethod(string.Empty, typeof(void),
                    !addThisArgForStatic && isStatic ? new[] {tField} : new[] {tElement, tField}, true);
            var ig = dm.GetILGenerator();

            short argIndex = 0;
            if (!isStatic) {
                ig.Emit(OpCodes.Ldarg, argIndex++);
                CastClass(ig, tElement, fieldInfo.DeclaringType);
            }
            if (!isGetter) {
                ig.Emit(OpCodes.Ldarg, argIndex++);
                CastClass(ig, tField, fieldInfo.FieldType);
                ig.Emit(isStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo);
            }
            else {
                ig.Emit(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);
                CastClass(ig, fieldInfo.FieldType, tField);
            }
            ig.Emit(OpCodes.Ret);
            return dm.CreateDelegate(delegateType);
        }

        #region inner classes

        struct HelperKey {
            public bool Equals(HelperKey other) {
                var simpleattr = type == other.type
                                 && string.Equals(handlerName, other.handlerName)
                                 && handlerType == other.handlerType
                                 && parametersCount == other.parametersCount
                                 && callVirtIfNeeded == other.callVirtIfNeeded
                                 && hasTypeParameters == other.hasTypeParameters;
                if (!simpleattr)
                    return false;
                if (hasTypeParameters) {
                    if (typeParameters.Length != other.typeParameters.Length)
                        return false;
                    for (var i = 0; i < typeParameters.Length; i++) {
                        if (typeParameters[i] != other.typeParameters[i])
                            return false;
                    }
                }
                return true;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj))
                    return false;
                return obj is HelperKey && Equals((HelperKey) obj);
            }

            public override int GetHashCode() {
                return getHashCode;
            }

            int GetHashCodeInternal() {
                unchecked {
                    var hashCode = type != null ? type.GetHashCode() : 0;
                    hashCode = (hashCode*397) ^ (handlerName != null ? handlerName.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (handlerType != null ? handlerType.GetHashCode() : 0);
                    if (typeParameters != null)
                        foreach (var element in typeParameters)
                            hashCode = (hashCode*397) ^ (element != null ? element.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ callVirtIfNeeded.GetHashCode();
                    hashCode = (hashCode*397) ^ parametersCount.GetHashCode();
                    return hashCode;
                }
            }

            public HelperKey(Type type, string handlerName, Type handlerType, int? parametersCount,
                Type[] typeParameters, bool callVirtIfNeeded) {
                this.type = type;
                this.handlerName = handlerName;
                this.handlerType = handlerType;

                this.parametersCount = parametersCount;
                this.typeParameters = typeParameters;
                this.callVirtIfNeeded = callVirtIfNeeded;
                hasTypeParameters = typeParameters != null;
                getHashCode = 0;
                getHashCode = GetHashCodeInternal();
            }

            public static bool operator ==(HelperKey left, HelperKey right) {
                return left.Equals(right);
            }

            public static bool operator !=(HelperKey left, HelperKey right) {
                return !left.Equals(right);
            }

            readonly Type type;
            readonly string handlerName;
            readonly Type handlerType;
            readonly int? parametersCount;
            readonly int getHashCode;
            readonly Type[] typeParameters;
            readonly bool hasTypeParameters;
            readonly bool callVirtIfNeeded;
        }

        #endregion
    }
}