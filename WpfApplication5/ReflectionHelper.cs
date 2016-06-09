using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DevExpress.Xpf.Core.Internal {
    public class ReflectionHelper {
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
                    for (int i = 0; i < typeParameters.Length; i++) {
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
                    int hashCode = (type != null ? type.GetHashCode() : 0);
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
                this.hasTypeParameters = typeParameters != null;
                this.getHashCode = 0;
                this.getHashCode = GetHashCodeInternal();
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

        Dictionary<HelperKey, object> InvokeInfo { get; set; }
        Dictionary<HelperKey, Type> PropertyTypeInfo { get; set; }

        public bool HasContent {
            get { return InvokeInfo.Count > 0; }
        }

        public ReflectionHelper() {
            InvokeInfo = new Dictionary<HelperKey, object>();
            PropertyTypeInfo = new Dictionary<HelperKey, Type>();
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

        static MethodInfo GetMethod(Type type, string methodName, BindingFlags bindingFlags, int? parametersCount = null,
            Type[] typeParameters = null) {
            if (parametersCount != null) {
                return
                    type.GetMethods(bindingFlags)
                        .Where(x => x.Name == methodName)
                        .First(x => x.GetParameters().Count() == parametersCount.Value);
            }
            if (typeParameters != null) {
                return type.GetMethods(bindingFlags).Where(x => x.Name == methodName).First(x => {
                    int i = 0;
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

        public static Action<object, Delegate, object, Delegate> CreatePushValueMethod(MethodInfo setValueDelegate,
            MethodInfo getValueDelegate) {
            DynamicMethod dm = new DynamicMethod(String.Empty, null,
                new Type[] {typeof(object), typeof(Delegate), typeof(object), typeof(Delegate)});
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

        internal static void CastClass(ILGenerator generator, Type sourceType, Type targetType) {
            if (Equals(null, targetType))
                return;
            if (sourceType == targetType)
                return;
            bool oneIsVoid = typeof(void) == sourceType || typeof(void) == targetType;
            bool sourceIsNull = Equals(null, sourceType);
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
            bool isStatic = mi.IsStatic;

            var thisArgType = instanceType ?? mi.DeclaringType;
            var returnType = mi.ReturnType;
            bool useTuple = false;
            Type[] delegateGenericArguments;
            bool skipArgumentLengthCheck = false;
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
                (isStatic ? resultParametersTypes : new Type[] {thisArgType}.Concat(resultParametersTypes)).ToArray();

            DynamicMethod dm;
            if (mi.IsVirtual && !callVirtIfNeeded)
                dm = new DynamicMethod(string.Empty, returnType, dynamicMethodParameterTypes, mi.DeclaringType, true);
            else
                dm = new DynamicMethod(string.Empty, returnType, dynamicMethodParameterTypes, true);
            var ig = dm.GetILGenerator();
            //AssemblyName asmName = new AssemblyName("abc");
            //var assemblyBuilder = Thread.GetDomain()
            //    .DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
            //var moduleBuilder = assemblyBuilder.DefineDynamicModule("abcd", "abc" + ".dll");
            //var typeBuilder = moduleBuilder.DefineType("abcdef");
            //var mt = typeBuilder.DefineMethod("abcdefg", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType,
            //    dynamicMethodParameterTypes);
            //var ig = mt.GetILGenerator();


            byte newLocalIndex = 0;
            List<LocalBuilder> localBuilders = new List<LocalBuilder>();
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
                //TODO
                //ig.Emit(OpCodes.Pop);
                if (isValueType) {
                    ig.Emit(OpCodes.Stloc_0);
                    ig.Emit(OpCodes.Ldloca_S, 0);
                }
            }
            short argumentIndex = mi.IsStatic ? (short) 0 : (short) 1;

            for (int parameterIndex = 0; parameterIndex < sourceParametersTypes.Length; parameterIndex++) {
                ig.Emit(OpCodes.Ldarg, argumentIndex);
                CastClass(ig, GetElementTypeIfNeeded(resultParametersTypes.ElementAt(parameterIndex)),
                    GetElementTypeIfNeeded(sourceParametersTypes[parameterIndex]));
                var parameter = mi.GetParameters()[argumentIndex - newLocalIndex];
                if (!parameter.IsOut) {
                    ig.Emit(OpCodes.Stloc, localBuilders[argumentIndex]);
                    ////TODO
                    //ig.EmitWriteLine(localBuilders[argumentIndex]);
                    if (!parameter.ParameterType.IsByRef)
                        ig.Emit(OpCodes.Ldloc, argumentIndex);
                    else
                        ig.Emit(OpCodes.Ldloca, (UInt16) argumentIndex);
                }
                else {
                    ig.Emit(OpCodes.Ldloca, (UInt16) argumentIndex);
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
                for (int parameterIndex = 0; parameterIndex < sourceParametersTypes.Length; parameterIndex++) {
                    if (sourceParametersTypes[parameterIndex].IsByRef) {
                        ig.Emit(OpCodes.Ldloc, localBuilders[newLocalIndex + parameterIndex]);
                        CastClass(ig, sourceParametersTypes[parameterIndex].GetElementType(),
                            resultParametersTypes.ElementAt(parameterIndex));
                    }
                }
                ig.Emit(OpCodes.Newobj, returnType.GetConstructors().First(x => x.GetParameters().Length > 0));
                //ig.Emit(OpCodes.Ldnull);
            }
            else {
                CastClass(ig, mi.ReturnType, returnType);
            }
            ig.Emit(OpCodes.Ret);
            //typeBuilder.CreateType();
            //assemblyBuilder.Save("fileeee");            
            //return null;
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
            bool hasReturnType = returnType != null && returnType != typeof(void);
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

        static Delegate CreateFieldGetterOrSetter<TElement, TField>(bool isGetter, Type delegateType, Type declaringType,
            string fieldName, BindingFlags bFlags) {
            FieldInfo fieldInfo = declaringType.GetField(fieldName, bFlags);
            bool isStatic = fieldInfo.IsStatic;
            DynamicMethod dm;
            if (isGetter)
                dm = new DynamicMethod(string.Empty, typeof(TField), new Type[] {typeof(TElement)}, true);
            else
                dm = new DynamicMethod(string.Empty, typeof(void), new Type[] {typeof(TElement), typeof(TField)}, true);
            var ig = dm.GetILGenerator();

            short argIndex = 0;
            if (!isStatic) {
                ig.Emit(OpCodes.Ldarg, argIndex++);
                CastClass(ig, typeof(TElement), fieldInfo.DeclaringType);
            }
            if (!isGetter) {
                ig.Emit(OpCodes.Ldarg, argIndex++);
                CastClass(ig, typeof(TField), fieldInfo.FieldType);
                ig.Emit(isStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo);
            }
            else {
                ig.Emit(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);
                CastClass(ig, fieldInfo.FieldType, typeof(TField));
            }
            ig.Emit(OpCodes.Ret);
            return dm.CreateDelegate(delegateType);
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
            Type type = entity.GetType();
            var key = new HelperKey(type, propertyName, null, null, null, true);
            if (!PropertyTypeInfo.TryGetValue(key, out propertyType)) {
                var pi = type.GetProperty(propertyName, bindingFlags);
                propertyType = pi.PropertyType;
            }
            return propertyType;
        }
    }

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

    public static class ReflectionHelperAttributes {
        [AttributeUsage(AttributeTargets.Property)]
        public class FieldAccessorAttribute : Attribute {}
    }

    public class ReflectionGeneratorMemberInfoInstance<TWrapper> where TWrapper : class {
        private readonly MemberInfo info;
        ReflectionGeneratorInstance<TWrapper> root;
        private BindingFlags? flags;

        public ReflectionGeneratorMemberInfoInstance(MemberInfo info, ReflectionGeneratorInstance<TWrapper> root) {
            this.info = info;
            this.root = root;
        }

        public ReflectionGeneratorInstance<TWrapper> EndMember() {
            return root;
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper> BindingFlags(BindingFlags flags) {
            root.WriteSetting(info, x => x.BindingFlags = flags);
            return this;
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper> FieldAccessor() {
            return this;
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper> Name(string name) {
            root.WriteSetting(info, x => x.Name = name);
            return this;
        }
    }

    public class BaseReflectionGeneratorInstanceSetting {
        private readonly BaseReflectionGeneratorInstance reflectionGeneratorInstance;

        public BaseReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance) {
            this.reflectionGeneratorInstance = reflectionGeneratorInstance;
        }

        public virtual BindingFlags GetBindingFlags() {
            return reflectionGeneratorInstance.defaultFlags;
        }

        public virtual string GetName(string defaultName) {
            return defaultName;
        }
    }

    public class ReflectionGeneratorInstanceSetting : BaseReflectionGeneratorInstanceSetting {
        public ReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance)
            : base(reflectionGeneratorInstance) {}

        public BindingFlags? BindingFlags { get; set; }
        public string Name { get; set; }

        public override BindingFlags GetBindingFlags() {
            return BindingFlags ?? base.GetBindingFlags();
        }

        public override string GetName(string defaultName) {
            return Name ?? base.GetName(defaultName);
        }
    }

    public class NullReflectionGeneratorInstanceSetting : BaseReflectionGeneratorInstanceSetting {
        public NullReflectionGeneratorInstanceSetting(BaseReflectionGeneratorInstance reflectionGeneratorInstance)
            : base(reflectionGeneratorInstance) {}
    }

    public class BaseReflectionGeneratorInstance {
        protected internal BindingFlags defaultFlags = BindingFlags.Instance | BindingFlags.Public;
    }

    public class ReflectionGeneratorInstance<TWrapper> : BaseReflectionGeneratorInstance where TWrapper : class {
        private object element;
        private ModuleBuilder moduleBuilder;
        private Dictionary<MemberInfo, ReflectionGeneratorInstanceSetting> settings;

        public ReflectionGeneratorInstance(ModuleBuilder builder, object element) {
            this.element = element;
            this.moduleBuilder = builder;
            settings = new Dictionary<MemberInfo, ReflectionGeneratorInstanceSetting>();
        }

        public ReflectionGeneratorInstance<TWrapper> DefaultBindingFlags(
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public) {
            defaultFlags = flags;
            return this;
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper> DefineMember(
            Expression<Func<TWrapper, object>> expression) {
            return new ReflectionGeneratorMemberInfoInstance<TWrapper>((expression.Body as MemberExpression).Member,
                this);
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper> DefineMember(
            Expression<Action<TWrapper>> expression) {
            return new ReflectionGeneratorMemberInfoInstance<TWrapper>((expression.Body as MethodCallExpression).Method,
                this);
        }

        enum MemberInfoKind {
            Method,
            PropertyGetter,
            PropertySetter
        }

        public TWrapper Create() {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeof(TWrapper).Name + Guid.NewGuid(),
                TypeAttributes.Public,
                typeof(ReflectionGeneratedObject));

            typeBuilder.AddInterfaceImplementation(typeof(TWrapper));
            Type sourceType = element.GetType();
            List<object> ctorArgs = new List<object>();
            List<FieldInfo> ctorInfos = new List<FieldInfo>();
            var sourceObjectField = typeBuilder.DefineField("fieldSourceObject", sourceType, FieldAttributes.Family);
            ctorInfos.Add(sourceObjectField);
            ctorArgs.Add(element);

            foreach (var wrapperMethodInfo in typeof(TWrapper).GetMethods()) {
                if (wrapperMethodInfo.IsSpecialName)
                    continue;
                DefineMethod<TWrapper>(typeBuilder, wrapperMethodInfo, ctorInfos, ctorArgs, sourceType,
                    sourceObjectField, GetSetting(wrapperMethodInfo), MemberInfoKind.Method);
            }
            foreach (PropertyInfo propertyInfo in typeof(TWrapper).GetProperties()) {
                var setting = GetSetting(propertyInfo);
                if (propertyInfo.GetMethod != null)
                    DefineMethod<TWrapper>(typeBuilder, propertyInfo.GetMethod, ctorInfos, ctorArgs, sourceType,
                        sourceObjectField, setting, MemberInfoKind.PropertyGetter);
                if (propertyInfo.SetMethod != null)
                    DefineMethod<TWrapper>(typeBuilder, propertyInfo.SetMethod, ctorInfos, ctorArgs, sourceType,
                        sourceObjectField, setting, MemberInfoKind.PropertySetter);
            }
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                ctorArgs.Select(x => x.GetType()).ToArray());
            var ctorIlGenerator = ctor.GetILGenerator();
            ctorIlGenerator.Emit(OpCodes.Ldarg_0);
            ctorIlGenerator.Emit(OpCodes.Call, typeof(ReflectionGeneratedObject).GetConstructor(Type.EmptyTypes));
            for (byte i = 0; i < ctorArgs.Count; i++) {
                ctorIlGenerator.Emit(OpCodes.Ldarg_0);
                ctorIlGenerator.Emit(OpCodes.Ldarg, i + 1);
                ctorIlGenerator.Emit(OpCodes.Stfld, ctorInfos[i]);
            }
            ctorIlGenerator.Emit(OpCodes.Ret);

            var result = typeBuilder.CreateType();
            return (TWrapper) Activator.CreateInstance(result, ctorArgs.ToArray());
        }

        private static void DefineMethod<TWrapper>(TypeBuilder typeBuilder, MethodInfo wrapperMethodInfo,
            List<FieldInfo> ctorInfos,
            List<object> ctorArgs, Type sourceType, FieldBuilder sourceObjectField,
            BaseReflectionGeneratorInstanceSetting setting, MemberInfoKind method) {
            var sourceMethodInfo = sourceType.GetMethod(GetTargetName(wrapperMethodInfo, setting, method),
                setting.GetBindingFlags());
            FieldBuilder fieldInfo = null;
            if (sourceMethodInfo != null) {
                fieldInfo = typeBuilder.DefineField("field" + wrapperMethodInfo.Name, typeof(Delegate),
                    FieldAttributes.Family);
                ctorInfos.Add(fieldInfo);
                ctorArgs.Add(sourceMethodInfo);
            }

            var parameterTypes = wrapperMethodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
            var methodBuilder = typeBuilder.DefineMethod(wrapperMethodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual, wrapperMethodInfo.ReturnType,
                parameterTypes);
            var ilGenerator = methodBuilder.GetILGenerator();
            if (sourceMethodInfo == null) {
                //TODO non-found method exception
                ilGenerator.Emit(OpCodes.Ret);
                typeBuilder.DefineMethodOverride(methodBuilder, typeof(TWrapper).GetMethod(wrapperMethodInfo.Name));
                return;
            }
            var returnType = wrapperMethodInfo.ReturnType;
            bool useTuple = false;
            var delegateType = ReflectionHelper.MakeGenericDelegate(parameterTypes, ref returnType,
                typeof(object), out useTuple);
            var createsTuple = wrapperMethodInfo.ReturnType != returnType;
            if (createsTuple)
                ilGenerator.DeclareLocal(returnType);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fieldInfo);
            ilGenerator.Emit(OpCodes.Ldtoken, sourceType);
            ilGenerator.Emit(OpCodes.Ldtoken, delegateType);
            if (useTuple)
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
            else
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.EmitCall(OpCodes.Call, ReflectionGeneratedObject.GetDelegateMethodInfo, null);
            ReflectionHelper.CastClass(ilGenerator, typeof(Delegate), delegateType);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, sourceObjectField);
            for (byte i = 0; i < parameterTypes.Length; i++) {
                ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                var paramType = parameterTypes[i];
                if (paramType.IsByRef) {
                    LSTind(ilGenerator, paramType.GetElementType(), false);
                }
            }
            ilGenerator.EmitCall(OpCodes.Call, delegateType.GetMethod("Invoke"), null);
            if (createsTuple) {
                SyncTupleItems(parameterTypes.Select((x, i) => new Tuple<int, Type>(i, x)).Where(x => x.Item2.IsByRef),
                    returnType, wrapperMethodInfo.ReturnType != typeof(void), ilGenerator);
            }
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, typeof(TWrapper).GetMethod(wrapperMethodInfo.Name));
        }

        private static void SyncTupleItems(IEnumerable<Tuple<int, Type>> tuples, Type returnType, bool skipFirst,
            ILGenerator ilGenerator) {
            int index = skipFirst ? 1 : 0;
            ilGenerator.Emit(OpCodes.Stloc_0);
            if (skipFirst) {
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.EmitCall(OpCodes.Call, GetTupleItem(returnType, 0), null);
            }
            var tpls = tuples.ToArray();
            for (int i = 0; i < tpls.Length; i++) {
                var tuple = tpls[i];
                var value = (byte) tuple.Item1 + 1;
                ilGenerator.Emit(OpCodes.Ldarg, value);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.EmitCall(OpCodes.Call, GetTupleItem(returnType, i + index), null);
                LSTind(ilGenerator, tuple.Item2.GetElementType(), true);
            }
        }

        internal static void LSTind(ILGenerator generator, Type type, bool stind) {
            OpCode opCode = OpCodes.Stind_Ref;

            switch (Type.GetTypeCode(type)) {
                case TypeCode.Boolean:
                    opCode = stind ? OpCodes.Stind_I1 : OpCodes.Ldind_U1;
                    break;
                case TypeCode.SByte:
                case TypeCode.Byte:
                    opCode = stind ? OpCodes.Stind_I1 : OpCodes.Ldind_I1;
                    break;
                case TypeCode.Char:
                    opCode = stind ? OpCodes.Stind_I2 : OpCodes.Ldind_U2;
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    opCode = stind ? OpCodes.Stind_I2 : OpCodes.Ldind_I2;
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    opCode = stind ? OpCodes.Stind_I4 : OpCodes.Ldind_I4;
                    break;
                case TypeCode.Int64:
                    opCode = stind ? OpCodes.Stind_I8 : OpCodes.Ldind_I8;
                    break;
                case TypeCode.UInt64:
                    opCode = stind ? OpCodes.Stind_I8 : OpCodes.Ldind_I8;
                    break;
                case TypeCode.Single:
                    opCode = stind ? OpCodes.Stind_R4 : OpCodes.Ldind_R4;
                    break;
                case TypeCode.Double:
                    opCode = stind ? OpCodes.Stind_R8 : OpCodes.Ldind_R8;
                    break;
                default:
                    if (type.IsClass) {
                        if (!stind)
                            opCode = OpCodes.Ldind_Ref;
                    }
                    else {
                        generator.Emit(stind ? OpCodes.Stobj : OpCodes.Ldobj, type);
                        return;
                    }
                    break;
            }
            generator.Emit(opCode);
        }

        static MethodInfo GetTupleItem(Type type, int i) {
            return type.GetMethod($"get_Item{i + 1}");
        }

        private static string GetTargetName(MethodInfo wrapperMethodInfo, BaseReflectionGeneratorInstanceSetting setting,
            MemberInfoKind kind) {
            var result = setting.GetName(wrapperMethodInfo.Name);
            if (kind == MemberInfoKind.PropertyGetter && !result.StartsWith("get_"))
                return "get_" + result;
            if (kind == MemberInfoKind.PropertySetter && !result.StartsWith("set_"))
                return "set_" + result;
            return wrapperMethodInfo.Name;
        }

        private BaseReflectionGeneratorInstanceSetting GetSetting(MemberInfo wrapperMethodInfo, bool createNew = false) {
            ReflectionGeneratorInstanceSetting result;
            if (settings.TryGetValue(wrapperMethodInfo, out result))
                return result;
            if (createNew) {
                result = new ReflectionGeneratorInstanceSetting(this);
                settings[wrapperMethodInfo] = result;
                return result;
            }
            return new NullReflectionGeneratorInstanceSetting(this);
        }

        public void WriteSetting(MemberInfo info, Action<ReflectionGeneratorInstanceSetting> func) {
            var setting = (ReflectionGeneratorInstanceSetting) GetSetting(info, true);
            func(setting);
        }
    }

    public static class ReflectionGenerator {
        const string typesAssemblyName = "reflectiongeneratortypes";
        const string typesModuleName = "reflectiongeneratormodule";
        internal static ModuleBuilder moduleBuilder;
        private static AssemblyBuilder assemblyBuilder;

        static ReflectionGenerator() {
            AssemblyName asmName = new AssemblyName(typesAssemblyName);
            assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(typesModuleName, typesAssemblyName + ".dll");
        }

        public static ReflectionGeneratorInstance<TWrapper> Wrap2<TWrapper>(this object element) where TWrapper : class {
            return new ReflectionGeneratorInstance<TWrapper>(moduleBuilder, element);
        }

        public static void Save() {
            assemblyBuilder.Save("myasm.dll");
        }
    }
}