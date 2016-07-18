using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using ReflectionHelper;

namespace ReflectionFramework.Internal {
    internal enum MemberInfoKind {
        Method,
        PropertyGetter,
        PropertySetter
    }
    public class BaseReflectionGeneratorInstance {
        readonly object element;
        readonly Type elementType;
        readonly bool isStatic;
        readonly ModuleBuilder moduleBuilder;
        readonly Dictionary<MemberInfo, ReflectionGeneratorInstanceSetting> settings;

        protected object Element { get { return element; } }
        protected Type ElementType { get { return elementType; } }
        protected struct InstanceCacheKey {
            private Type elementType;
            private Type type;
            private int v;

            public InstanceCacheKey(Type elementType, Type type, int v) {
                this.elementType = elementType;
                this.type = type;
                this.v = v;
            }
        }

        protected static readonly Dictionary<InstanceCacheKey, Func<object, object>> CachedConstructors;

        static BaseReflectionGeneratorInstance() {
            CachedConstructors = new Dictionary<InstanceCacheKey, Func<object, object>>();
        }
        protected internal BindingFlags defaultFlags = BindingFlags.Instance | BindingFlags.Public;
        protected internal Type tWrapper;
        protected internal BaseReflectionGeneratorInstance(ModuleBuilder builder, object element, bool isStatic, Type tWrapper) {
            this.tWrapper = tWrapper;
            if (isStatic) {
                this.element = null;
                elementType = (Type)element;
            } else {
                this.element = element;
                elementType = this.element?.GetType();
            }
            moduleBuilder = builder;
            this.isStatic = isStatic;
            settings = new Dictionary<MemberInfo, ReflectionGeneratorInstanceSetting>();
        }

        internal object CreateInternal() {
            return CreateImpl();
        }
        protected virtual object CreateOverride() {
            return CreateImpl();
        }
        protected virtual object CreateInstanceOverride(Type result, List<object> ctorArgs) {
            return CreateInstance(result, ctorArgs);
        }

        protected object CreateImpl() {
            Log.Write($"Generating {tWrapper}");
            var typeBuilder = moduleBuilder.DefineType(tWrapper.Name + Guid.NewGuid(),
                TypeAttributes.Public,
                typeof(ReflectionGeneratedObject));

            typeBuilder.AddInterfaceImplementation(tWrapper);
            var sourceType = elementType;
            var ctorArgs = new List<object>();
            var ctorInfos = new List<FieldInfo>();
            var sourceObjectField = typeBuilder.DefineField("fieldSourceObject", sourceType, FieldAttributes.Family);
            ctorInfos.Add(sourceObjectField);
            ctorArgs.Add(element);

            foreach (var wrapperMethodInfo in tWrapper.GetMethods()) {
                if (wrapperMethodInfo.IsSpecialName)
                    continue;
                DefineMethod(typeBuilder, wrapperMethodInfo, null, ctorInfos, ctorArgs, sourceType,
                    sourceObjectField, GetSetting(wrapperMethodInfo), MemberInfoKind.Method, isStatic);
            }
            foreach (var propertyInfo in tWrapper.GetProperties()) {
                var setting = GetSetting(propertyInfo);
                var field = setting.FieldAccessor(propertyInfo);
                var getMethod = propertyInfo.GetGetMethod(true);
                var setMethod = propertyInfo.GetSetMethod(true);
                if (getMethod != null)
                    if (field)
                        DefineFieldGetterOrSetter(typeBuilder, propertyInfo, getMethod, ctorInfos, ctorArgs, sourceType,
                            sourceObjectField, setting, MemberInfoKind.PropertyGetter, isStatic);
                    else
                        DefineMethod(typeBuilder, getMethod, propertyInfo, ctorInfos, ctorArgs, sourceType,
                            sourceObjectField, setting, MemberInfoKind.PropertyGetter, isStatic);
                if (setMethod != null)
                    if (field)
                        DefineFieldGetterOrSetter(typeBuilder, propertyInfo, setMethod, ctorInfos, ctorArgs, sourceType,
                            sourceObjectField, setting, MemberInfoKind.PropertySetter, isStatic);
                    else
                        DefineMethod(typeBuilder, setMethod, propertyInfo, ctorInfos, ctorArgs, sourceType,
                            sourceObjectField, setting, MemberInfoKind.PropertySetter, isStatic);
            }
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                ctorInfos.Select(x => x.FieldType).ToArray());
            var ctorIlGenerator = ctor.GetILGenerator();
            ctorIlGenerator.Emit(OpCodes.Ldarg_0);
            ctorIlGenerator.Emit(OpCodes.Ldarg_1);
            ctorIlGenerator.Emit(OpCodes.Call, typeof(ReflectionGeneratedObject).GetConstructor(new Type[] {typeof(object)}));
            for (byte i = 0; i < ctorArgs.Count; i++) {
                ctorIlGenerator.Emit(OpCodes.Ldarg_0);
                ctorIlGenerator.Emit(OpCodes.Ldarg, i + 1);
                ctorIlGenerator.Emit(OpCodes.Stfld, ctorInfos[i]);
            }
            ctorIlGenerator.Emit(OpCodes.Ret);

            var result = typeBuilder.CreateType();
            return CreateInstance(result, ctorArgs);
        }
        protected object CachedCreateImpl() {
            InstanceCacheKey icc = new InstanceCacheKey(ElementType, tWrapper, GetSettingCode());
            Func<object, object> result;
            if (CachedConstructors.TryGetValue(icc, out result)) {
                return result(Element);
            }
            return CreateImpl();
        }        

        protected object CachedCreateInstance(Type result, List<object> ctorArgs) {
            InstanceCacheKey icc = new InstanceCacheKey(ElementType, tWrapper, GetSettingCode());
            CachedConstructors[icc] = CreateConstructor(result, ctorArgs);
            return CreateInstance(result, ctorArgs);
        }
        protected object CreateInstance(Type result, List<object> ctorArgs) {
            return Activator.CreateInstance(result, ctorArgs.ToArray());
        }

        Func<object, object> CreateConstructor(Type result, List<object> ctorArgs) {
            return (x) => Activator.CreateInstance(result, new[] { x }.Concat(ctorArgs.Skip(1)).ToArray());
        }

        

        void DefineFieldGetterOrSetter(TypeBuilder typeBuilder, PropertyInfo propertyInfo, MethodInfo wrapperMethodInfo,
            List<FieldInfo> ctorInfos,
            List<object> ctorArgs, Type sourceType, FieldBuilder sourceObjectField,
            BaseReflectionGeneratorInstanceSetting setting, MemberInfoKind method, bool isStatic) {
            var sourceFieldInfo =
                sourceType.GetField(GetTargetName(propertyInfo.Name, setting, MemberInfoKind.Method, propertyInfo),
                    setting.GetBindingFlags(wrapperMethodInfo) | (isStatic ? BindingFlags.Static : 0));
            FieldBuilder fieldInfo = null;
            if (sourceFieldInfo != null) {
                fieldInfo = typeBuilder.DefineField("field" + wrapperMethodInfo.Name, sourceFieldInfo.GetType(),
                    FieldAttributes.Family);
                ctorInfos.Add(fieldInfo);
                ctorArgs.Add(sourceFieldInfo);
            }

            var parameterTypes = wrapperMethodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
            var methodBuilder = typeBuilder.DefineMethod(wrapperMethodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual, wrapperMethodInfo.ReturnType,
                parameterTypes);
            var ilGenerator = methodBuilder.GetILGenerator();
            var returnType = wrapperMethodInfo.ReturnType;
            bool wrapReturnType = method == MemberInfoKind.PropertyGetter && ShouldWrapType(returnType);
            bool wrapParameterType = method == MemberInfoKind.PropertySetter && !wrapReturnType && ShouldWrapType(parameterTypes[0]);
            Type unwrappedReturnType, unwrappedParameterType;
            Type[] unwrappedParameterTypes = parameterTypes;
            unwrappedReturnType = wrapReturnType ? typeof(object) : returnType;
            if (wrapParameterType) {
                unwrappedParameterType = sourceFieldInfo.FieldType;
                unwrappedParameterTypes = new Type[] {unwrappedParameterType};
            } else {
                unwrappedParameterType = null;
            }
            
            var useTuple = false;
            var delegateType = ReflectionHelper.MakeGenericDelegate(unwrappedParameterTypes, ref unwrappedReturnType,
                isStatic ? null : typeof(object), out useTuple);
            var fallbackMode = sourceFieldInfo == null;
            if (fallbackMode) {
                PrepareFallback(typeBuilder, wrapperMethodInfo, ctorInfos, ctorArgs, setting, ilGenerator, method);
            } else {
                if (wrapReturnType) {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldtoken, returnType);
                }
                ilGenerator.Emit(OpCodes.Ldarg_0);
                Ldfld(ilGenerator, fieldInfo);
                ilGenerator.Emit(OpCodes.Ldtoken, delegateType);
                ilGenerator.Emit(OpCodes.Ldtoken, typeof(object));
                if (wrapParameterType)
                    ilGenerator.Emit(OpCodes.Ldtoken, typeof(object));
                else
                    ilGenerator.Emit(OpCodes.Ldtoken, sourceFieldInfo.FieldType);
                if (isStatic)
                    ilGenerator.Emit(OpCodes.Ldc_I4_1);
                else
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.EmitCall(OpCodes.Call,
                    method == MemberInfoKind.PropertyGetter
                        ? ReflectionGeneratedObject.GetFieldGetterMethodInfo
                        : ReflectionGeneratedObject.GetFieldSetterMethodInfo, null);
            }
            ReflectionHelper.CastClass(ilGenerator, typeof(Delegate), delegateType);            
            if (!isStatic) {
                Ldfld(ilGenerator, sourceObjectField);
            }
            for (byte i = 0; i < parameterTypes.Length; i++) {
                if (wrapParameterType) {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                }
                ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                if (wrapParameterType) {
                    ilGenerator.EmitCall(OpCodes.Call, ReflectionGeneratedObject.UnwrapMethodInfo, null);
                    ReflectionHelper.CastClass(ilGenerator, parameterTypes[0], unwrappedParameterType);
                }
            }
            ilGenerator.EmitCall(OpCodes.Call, delegateType.GetMethod("Invoke"), null);
            if (wrapReturnType) {
                ilGenerator.EmitCall(OpCodes.Call, ReflectionGeneratedObject.WrapMethodInfo, null);
                ReflectionHelper.CastClass(ilGenerator, typeof(object), returnType);
            }
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, tWrapper.GetMethod(wrapperMethodInfo.Name));            
        }

        static void Ldfld(ILGenerator generator, FieldBuilder fieldBuilder) {
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, fieldBuilder);
        }

        static bool ShouldWrapType(Type type) {
            if(type.IsByRef)
                return ShouldWrapType(type.GetElementType());
            return type.GetCustomAttributes(typeof(ReflectionHelperAttributes.WrapperAttribute), false).Any();
        }

        static readonly Type tpObject = typeof(object).Assembly.GetType(typeof(object).FullName + "&");
        void DefineMethod(TypeBuilder typeBuilder, MethodInfo wrapperMethodInfo, MemberInfo baseMemberInfo,
            List<FieldInfo> ctorInfos,
            List<object> ctorArgs, Type sourceType, FieldBuilder sourceObjectField,
            BaseReflectionGeneratorInstanceSetting setting, MemberInfoKind method, bool isStatic) {
            var sourceMethodInfo = sourceType.GetMethod(GetTargetName(wrapperMethodInfo.Name, setting, method, baseMemberInfo ?? wrapperMethodInfo),
                setting.GetBindingFlags(baseMemberInfo ?? wrapperMethodInfo) | (isStatic ? BindingFlags.Static : 0));
            FieldBuilder fieldInfo = null;
            if (sourceMethodInfo != null) {
                fieldInfo = typeBuilder.DefineField("field" + wrapperMethodInfo.Name, sourceMethodInfo.GetType(),
                    FieldAttributes.Family);
                ctorInfos.Add(fieldInfo);
                ctorArgs.Add(sourceMethodInfo);
            }

            var parameterTypes = wrapperMethodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
            var genericParameters = wrapperMethodInfo.GetGenericArguments();
            var methodBuilder = typeBuilder.DefineMethod(wrapperMethodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual, wrapperMethodInfo.ReturnType,
                parameterTypes);
            GenericTypeParameterBuilder[] genericParameterBuilders = null;
            if (genericParameters.Length > 0)
                genericParameterBuilders =
                    methodBuilder.DefineGenericParameters(genericParameters.Select(x => x.Name).ToArray());
            Type[] updatedParameterTypes = new Type[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; i++) {
                var currentType = parameterTypes[i];
                if (ShouldWrapType(currentType))
                    updatedParameterTypes[i] = currentType.IsByRef ? tpObject : typeof(object);
                else
                    updatedParameterTypes[i] = currentType;
            }
            var ilGenerator = methodBuilder.GetILGenerator();            
            var returnType = wrapperMethodInfo.ReturnType;
            var wrapReturnType = ShouldWrapType(returnType);
            var unwrappedReturnTupe = wrapReturnType ? typeof(object) : returnType;
            var useTuple = false;
            var delegateType = ReflectionHelper.MakeGenericDelegate(updatedParameterTypes, ref unwrappedReturnTupe,
                isStatic ? null : typeof(object), out useTuple);
            LocalBuilder tupleLocalBuilder = null;
            if (useTuple)
                tupleLocalBuilder = ilGenerator.DeclareLocal(unwrappedReturnTupe);
            var fallbackMode = sourceMethodInfo == null;
            if (fallbackMode) {
                PrepareFallback(typeBuilder, wrapperMethodInfo, ctorInfos, ctorArgs, setting, ilGenerator, method);
            } else {
                if (wrapReturnType) {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldtoken, returnType);
                }                    
                ilGenerator.Emit(OpCodes.Ldarg_0);
                Ldfld(ilGenerator, fieldInfo);
                ilGenerator.Emit(OpCodes.Ldtoken, sourceType);
                ilGenerator.Emit(OpCodes.Ldtoken, delegateType);
                ilGenerator.Emit(useTuple ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                var methodInfo = ReflectionGeneratedObject.GetDelegateMethodInfo;
                if (genericParameters.Length > 0) {
                    ilGenerator.Emit(OpCodes.Ldc_I4, genericParameters.Length);
                    ilGenerator.Emit(OpCodes.Newarr, typeof(Type));
                    for (var i = 0; i < genericParameters.Length; i++) {
                        ilGenerator.Emit(OpCodes.Dup);
                        ilGenerator.Emit(OpCodes.Ldc_I4, i);
                        ilGenerator.Emit(OpCodes.Ldtoken, genericParameterBuilders[i]);
                        ilGenerator.Emit(OpCodes.Stelem_Ref);
                    }
                    methodInfo = ReflectionGeneratedObject.GetGenericDelegateMethodInfo;
                }
                ilGenerator.EmitCall(OpCodes.Call, methodInfo, null);
            }
            ReflectionHelper.CastClass(ilGenerator, typeof(Delegate), delegateType);
            if (!isStatic) {
                Ldfld(ilGenerator, sourceObjectField);
            }
            for (byte i = 0; i < updatedParameterTypes.Length; i++) {
                var paramType = updatedParameterTypes[i];
                if (parameterTypes[i] != updatedParameterTypes[i]) {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                    if (paramType.IsByRef)
                        LSTind(ilGenerator, paramType.GetElementType(), false);
                    ilGenerator.EmitCall(OpCodes.Call, ReflectionGeneratedObject.UnwrapMethodInfo, null);
                } else {
                    ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                    if (paramType.IsByRef)
                        LSTind(ilGenerator, paramType.GetElementType(), false);
                }                
            }
            ilGenerator.EmitCall(OpCodes.Call, delegateType.GetMethod("Invoke"), null);

            if (useTuple) {
                SyncTupleItems(updatedParameterTypes.Select((x, i) => new Tuple<int, Type, Type>(i, x, parameterTypes[i])).Where(x => x.Item2.IsByRef),
                    unwrappedReturnTupe, wrapperMethodInfo.ReturnType != typeof(void), ilGenerator, tupleLocalBuilder);
            }
            if (wrapReturnType) {
                ilGenerator.EmitCall(OpCodes.Call, ReflectionGeneratedObject.WrapMethodInfo, null);
                ReflectionHelper.CastClass(ilGenerator, typeof(object), returnType);
            }
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, tWrapper.GetMethod(wrapperMethodInfo.Name));
        }

        static void PrepareFallback(TypeBuilder typeBuilder, MethodInfo wrapperMethodInfo,
            List<FieldInfo> ctorInfos, List<object> ctorArgs,
            BaseReflectionGeneratorInstanceSetting setting, ILGenerator ilGenerator, MemberInfoKind infoKind) {
            var fallback = setting.GetFallback(infoKind);
            var fallbackField = typeBuilder.DefineField("field" + wrapperMethodInfo.Name + "fallback",
                fallback.GetType(), FieldAttributes.Family);
            ctorInfos.Add(fallbackField);
            ctorArgs.Add(fallback);
            Ldfld(ilGenerator, fallbackField);
        }


        static void SyncTupleItems(IEnumerable<Tuple<int, Type, Type>> tuples, Type returnType, bool skipFirst,
            ILGenerator ilGenerator, LocalBuilder tupleLocalBuilder) {
            var index = skipFirst ? 1 : 0;
            ilGenerator.Emit(OpCodes.Stloc, tupleLocalBuilder);
            if (skipFirst) {
                ilGenerator.Emit(OpCodes.Ldloc, tupleLocalBuilder);
                ilGenerator.EmitCall(OpCodes.Call, GetTupleItem(returnType, 0), null);
            }
            var tpls = tuples.ToArray();
            for (var i = 0; i < tpls.Length; i++) {
                var tuple = tpls[i];
                var value = (byte)tuple.Item1 + 1;
                ilGenerator.Emit(OpCodes.Ldarg, value);
                if (tuple.Item2 != tuple.Item3) {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldtoken, tuple.Item3.GetElementType());
                }                
                ilGenerator.Emit(OpCodes.Ldloc, tupleLocalBuilder);
                ilGenerator.EmitCall(OpCodes.Call, GetTupleItem(returnType, i + index), null);
                if (tuple.Item2 != tuple.Item3) {
                    ilGenerator.EmitCall(OpCodes.Call, ReflectionGeneratedObject.WrapMethodInfo, null);
                    ReflectionHelper.CastClass(ilGenerator, tuple.Item2.GetElementType(), tuple.Item3.GetElementType());
                }
                LSTind(ilGenerator, tuple.Item3.GetElementType(), true);
            }
        }

        internal static void LSTind(ILGenerator generator, Type type, bool stind) {
            var opCode = OpCodes.Stind_Ref;

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
                    } else {
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

        static string GetTargetName(string wrapperMethodInfo, BaseReflectionGeneratorInstanceSetting setting,
            MemberInfoKind kind, MemberInfo memberInfo) {
            var result = setting.GetName(wrapperMethodInfo, memberInfo);
            if (kind == MemberInfoKind.PropertyGetter && !result.StartsWith("get_"))
                return "get_" + result;
            if (kind == MemberInfoKind.PropertySetter && !result.StartsWith("set_"))
                return "set_" + result;
            return result;
        }

        BaseReflectionGeneratorInstanceSetting GetSetting(MemberInfo wrapperMethodInfo, bool createNew = false) {
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

        protected int GetSettingCode() {
            unchecked {
                var result = 0;
                foreach (var setting in settings.Values) {
                    result = (result * 397) ^ setting.ComputeKey();
                }
                return result;
            }
        }

        internal void WriteSetting(MemberInfo info, Action<ReflectionGeneratorInstanceSetting> func) {
            var setting = (ReflectionGeneratorInstanceSetting)GetSetting(info, true);
            func(setting);
        }
    }

    public class ReflectionGeneratorInstanceWrapper<TWrapper> : ReflectionGeneratorWrapper<TWrapper> {                
        public ReflectionGeneratorInstanceWrapper(ModuleBuilder builder, object element) : base(builder, element, false) {}

        public ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, ReflectionGeneratorInstanceWrapper<TWrapper>> DefineProperty(Expression<Func<TWrapper, object>> expression) {
            return DefineProperty<ReflectionGeneratorInstanceWrapper<TWrapper>>(expression);
        }
        public ReflectionGeneratorMemberInfoInstance<TWrapper, ReflectionGeneratorInstanceWrapper<TWrapper>> DefineMethod(Expression<Action<TWrapper>> expression) {
            return DefineMethod<ReflectionGeneratorInstanceWrapper<TWrapper>>(expression);
        }

        protected override object CreateInstanceOverride(Type result, List<object> ctorArgs) {
            return CachedCreateInstance(result, ctorArgs);
        }
        protected override object CreateOverride() {
            return CachedCreateImpl();
        }
    }

    public class ReflectionGeneratorStaticWrapper<TWrapper> : ReflectionGeneratorWrapper<TWrapper> {
        public ReflectionGeneratorStaticWrapper(ModuleBuilder builder, object element) : base(builder, element, true) {}
        public ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, ReflectionGeneratorStaticWrapper<TWrapper>> DefineProperty(Expression<Func<TWrapper, object>> expression) {
            return DefineProperty<ReflectionGeneratorStaticWrapper<TWrapper>>(expression);
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper, ReflectionGeneratorStaticWrapper<TWrapper>> DefineMethod(Expression<Action<TWrapper>> expression) {
            return DefineMethod<ReflectionGeneratorStaticWrapper<TWrapper>>(expression);
        }
    }
    public class ReflectionGeneratorWrapper<TWrapper> : BaseReflectionGeneratorInstance {        

        public ReflectionGeneratorWrapper(ModuleBuilder builder, object element, bool isStatic) : base(builder, element, isStatic, typeof(TWrapper)) {            
        }

        public TWrapper Create() {
            return (TWrapper)CreateOverride();
        }

        public ReflectionGeneratorWrapper<TWrapper> DefaultBindingFlags(
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public) {
            defaultFlags = flags;
            return this;
        }

        protected ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> DefineProperty<TReflectionGeneratorWrapper>(
            Expression<Func<TWrapper, object>> expression) where TReflectionGeneratorWrapper : ReflectionGeneratorWrapper<TWrapper> {
            if (expression.Body is MemberExpression)
                return
                    new ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper>(
                        (expression.Body as MemberExpression).Member,
                        (TReflectionGeneratorWrapper)this);
            if (expression.Body is UnaryExpression)
                return
                    new ReflectionGeneratorPropertyMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper>(
                        ((expression.Body as UnaryExpression).Operand as MemberExpression).Member,
                        (TReflectionGeneratorWrapper)this);
            return null;
        }

        protected ReflectionGeneratorMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper> DefineMethod<TReflectionGeneratorWrapper>(
            Expression<Action<TWrapper>> expression) where TReflectionGeneratorWrapper : ReflectionGeneratorWrapper<TWrapper> {
            return new ReflectionGeneratorMemberInfoInstance<TWrapper, TReflectionGeneratorWrapper>((expression.Body as MethodCallExpression).Method,
                (TReflectionGeneratorWrapper)this);
        }        
    }    
}