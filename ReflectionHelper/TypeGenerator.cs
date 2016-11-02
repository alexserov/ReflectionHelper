using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Cache;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Text;
using ReflectionFramework.Attributes;

namespace ReflectionFramework.Internal {
    internal enum MemberInfoKind {
        Method,
        PropertyGetter,
        PropertySetter
    }
    public class BaseReflectionHelperInterfaceWrapperGenerator {
        readonly object element;
        readonly Type elementType;
        readonly bool isStatic;
        readonly ModuleBuilder moduleBuilder;
        readonly Dictionary<MemberInfo, ReflectionHelperInterfaceWrapperSetting> settings;

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

        static BaseReflectionHelperInterfaceWrapperGenerator() {
            CachedConstructors = new Dictionary<InstanceCacheKey, Func<object, object>>();            
            SubscribeTypeResolve();
        }

        [SecuritySafeCritical]
        static void SubscribeTypeResolve() {
            AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;
        }
        private static Assembly CurrentDomain_TypeResolve(object sender, ResolveEventArgs args)
        {
            return null;            
        }

        protected internal BindingFlags defaultFlags = BindingFlags.Instance | BindingFlags.Public;        
        protected internal  ReflectionHelperFallbackMode defaultFallbackMode = ReflectionHelperFallbackMode.Default;
        protected internal Type tWrapper;
        protected internal BaseReflectionHelperInterfaceWrapperGenerator(ModuleBuilder builder, object element, bool isStatic, Type tWrapper) {
            this.tWrapper = tWrapper;
            if (isStatic) {
                this.element = null;
                elementType = (Type)element;
            } else {
                this.element = element;
                elementType = this.element != null ? this.element.GetType() : null;
            }
            moduleBuilder = builder;
            this.isStatic = isStatic;
            settings = new Dictionary<MemberInfo, ReflectionHelperInterfaceWrapperSetting>();
        }

        internal object CreateInternal() {
            return CachedCreateImpl();
        }                

        object CreateImpl() {
            if (!CheckAssignableFromAttribute())
                return CachedCreateInstance(null, null);            
            var typeBuilder = moduleBuilder.DefineType(tWrapper.Name + Guid.NewGuid(),
                TypeAttributes.Public,
                typeof(ReflectionHelperInterfaceWrapper));

            typeBuilder.AddInterfaceImplementation(tWrapper);
            var sourceType = elementType;
            var ctorArgs = new List<object>();
            var ctorInfos = new List<FieldInfo>();            
            var sourceObjectField = typeBuilder.DefineField("fieldSourceObject", typeof(object), FieldAttributes.Private);
            ctorInfos.Add(sourceObjectField);
            ctorArgs.Add(element);            

            foreach (var wrapperMethodInfo in GetMethods()) {
                if (wrapperMethodInfo.IsSpecialName)
                    continue;
                DefineMethod(typeBuilder, wrapperMethodInfo, null, ctorInfos, ctorArgs, sourceType,
                    sourceObjectField, GetSetting(wrapperMethodInfo), MemberInfoKind.Method, isStatic);
            }
            foreach (var propertyInfo in GetProperties()) {
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
                ctorInfos.Select(x => typeof(object)).ToArray());
            var ctorIlGenerator = ctor.GetILGenerator();
            ctorIlGenerator.Emit(OpCodes.Ldarg_0);
            ctorIlGenerator.Emit(OpCodes.Ldarg_1);
            ctorIlGenerator.Emit(OpCodes.Call, typeof(ReflectionHelperInterfaceWrapper).GetConstructor(new Type[] {typeof(object)}));
            for (byte i = 0; i < ctorArgs.Count; i++) {
                ctorIlGenerator.Emit(OpCodes.Ldarg_0);
                ctorIlGenerator.Emit(OpCodes.Ldarg, i + 1);
                ctorIlGenerator.Emit(OpCodes.Stfld, ctorInfos[i]);
            }
            ctorIlGenerator.Emit(OpCodes.Ret);

            var result = typeBuilder.CreateType();
            return CachedCreateInstance(result, ctorArgs);
        }        
        bool CheckAssignableFromAttribute() {
            var assignableFrom = IterateInterfaces().SelectMany(x=>x.GetCustomAttributes(typeof(AssignableFromAttribute), true)).Distinct().OfType<AssignableFromAttribute>();
            var assignable = assignableFrom.Where(x => !x.Inverse).Select(x=>x.GetTypeName()).ToList();
            var unassignable = assignableFrom.Where(x => x.Inverse).Select(x => x.GetTypeName()).ToList();
            var tEnumerator = FlatternType(ElementType, true).GetEnumerator();
            while (tEnumerator.MoveNext()) {
                var currentType = tEnumerator.Current;
                if (assignable.Count == 0 && unassignable.Count == 0)
                    return true;
                if (assignable.Contains(currentType.FullName)) {
                    assignable.Remove(currentType.FullName);
                }
                if (unassignable.Contains(currentType.FullName))
                    return false;
            }
            return assignable.Count==0;
        }

        IEnumerable<Type> FlatternType(Type t, bool flatternInterfaces) {
            if (t == null)
                yield break;
            if (flatternInterfaces)
                foreach (var tInterface in t.GetInterfaces()) {
                    yield return tInterface;
                }
            while (t != null) {
                yield return t;
                t = t.BaseType;
            }

        }

        IEnumerable<PropertyInfo> GetProperties() {
            return IterateInterfaces().SelectMany(x => x.GetProperties());
        }

        IEnumerable<Type> IterateInterfaces() {
            yield return tWrapper;
            foreach (var type in tWrapper.GetInterfaces()) {
                yield return type;
            }
        }

        IEnumerable<MethodInfo> GetMethods() {
            return IterateInterfaces().SelectMany(x => x.GetMethods());
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
        object CreateInstance(Type result, List<object> ctorArgs) {
            if (result == null)
                return null;
            return Activator.CreateInstance(result, ctorArgs.ToArray());
        }

        Func<object, object> CreateConstructor(Type result, List<object> ctorArgs) {
            if (result == null)
                return x => null;
            return (x) => Activator.CreateInstance(result, new[] { x }.Concat(ctorArgs.Skip(1)).ToArray());
        }

        

        void DefineFieldGetterOrSetter(TypeBuilder typeBuilder, PropertyInfo propertyInfo, MethodInfo wrapperMethodInfo,
            List<FieldInfo> ctorInfos,
            List<object> ctorArgs, Type sourceType, FieldBuilder sourceObjectField,
            BaseReflectionHelperInterfaceWrapperSetting setting, MemberInfoKind method, bool isStatic) {
            var sourceFieldInfo =
                sourceType.GetField(GetTargetName(propertyInfo.Name, setting, MemberInfoKind.Method, propertyInfo),
                    setting.GetBindingFlags(wrapperMethodInfo, propertyInfo) | (isStatic ? BindingFlags.Static : 0));
            FieldBuilder fieldInfo = null;
            if (sourceFieldInfo != null) {
                fieldInfo = typeBuilder.DefineField("field" + wrapperMethodInfo.Name, sourceFieldInfo.GetType(),
                    FieldAttributes.Private);
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
            var delegateInvoke = delegateType.GetMethod("Invoke");
            var fallbackMode = sourceFieldInfo == null;
            if (fallbackMode) {
                if(!PrepareFallback(typeBuilder, wrapperMethodInfo, ctorInfos, ctorArgs, setting, ilGenerator, method, delegateInvoke, isStatic, propertyInfo)) {
                    ilGenerator.Emit(OpCodes.Ret);
                    return;
                }          
            } else {                
                ilGenerator.Emit(OpCodes.Ldarg_0);
                Ldfld(ilGenerator, fieldInfo);
                TypeOf(ilGenerator, delegateType);
                TypeOf(ilGenerator, typeof(object));
                if (wrapParameterType || wrapReturnType)
                    TypeOf(ilGenerator, typeof(object));
                else
                    TypeOf(ilGenerator, sourceFieldInfo.FieldType);
                if (isStatic)
                    ilGenerator.Emit(OpCodes.Ldc_I4_1);
                else
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                EmitCall(ilGenerator,OpCodes.Call,
                    method == MemberInfoKind.PropertyGetter
                        ? ReflectionHelperInterfaceWrapper.GetFieldGetterMethodInfo
                        : ReflectionHelperInterfaceWrapper.GetFieldSetterMethodInfo, null);
            }
            if (!isStatic) {
                Ldfld(ilGenerator, sourceObjectField);
            }
            for (byte i = 0; i < parameterTypes.Length; i++) {                
                ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                if (wrapParameterType) {
                    EmitCall(ilGenerator,OpCodes.Call, ReflectionHelperInterfaceWrapper.UnwrapMethodInfo, null);
                }
            }
            EmitCall(ilGenerator,OpCodes.Call, delegateInvoke, null);
            if (wrapReturnType) {
                TypeOf(ilGenerator, returnType);
                EmitCall(ilGenerator,OpCodes.Call, ReflectionHelperInterfaceWrapper.WrapMethodInfo, null);
            }
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, tWrapper.GetMethod(wrapperMethodInfo.Name));            
        }

        static MethodInfo getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public|BindingFlags.Static);
        static void TypeOf(ILGenerator generator, Type type) {
            generator.Emit(OpCodes.Ldtoken, type);
            generator.Emit(OpCodes.Call, getTypeFromHandle);
        }
        static void EmitCall(ILGenerator generator, OpCode opCode, MethodInfo info, Type[] types) {
            if (info.CallingConvention.HasFlag(CallingConventions.VarArgs))
                generator.EmitCall(opCode, info, types);
            else {
                generator.Emit(opCode, info);
            }
        }
        static void Ldfld(ILGenerator generator, FieldBuilder fieldBuilder) {
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, fieldBuilder);
        }

        static bool ShouldWrapType(Type type) {
            if(type.IsByRef)
                return ShouldWrapType(type.GetElementType());
            return type.GetCustomAttributes(typeof(WrapperAttribute), false).Any();
        }

        static readonly Type tpObject = typeof(object).Assembly.GetType(typeof(object).FullName + "&");
        void DefineMethod(TypeBuilder typeBuilder, MethodInfo wrapperMethodInfo, MemberInfo baseMemberInfo,
            List<FieldInfo> ctorInfos,
            List<object> ctorArgs, Type sourceType, FieldBuilder sourceObjectField,
            BaseReflectionHelperInterfaceWrapperSetting setting, MemberInfoKind method, bool isStatic) {
            var sourceMethodIsInterface = GetIsInterface(setting, wrapperMethodInfo.Name, baseMemberInfo ?? wrapperMethodInfo);
            var sourceMethodName = GetTargetName(wrapperMethodInfo.Name, setting, method, baseMemberInfo ?? wrapperMethodInfo);
            var sourceMethodBindingFalgs = setting.GetBindingFlags(baseMemberInfo, wrapperMethodInfo) | (isStatic ? BindingFlags.Static : 0);
            var sourceMethodInfo = (sourceMethodIsInterface ? sourceType.GetInterfaces() : FlatternType(sourceType, false)).Select(x => x.GetMethod(sourceMethodName, sourceMethodBindingFalgs)).FirstOrDefault(x => x != null);
            FieldBuilder fieldInfo = null;
            if (sourceMethodInfo != null) {
                fieldInfo = typeBuilder.DefineField("field" + wrapperMethodInfo.Name, sourceMethodInfo.GetType(),
                    FieldAttributes.Private);
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
            var delegateInvoke = delegateType.GetMethod("Invoke");
            LocalBuilder tupleLocalBuilder = null;
            if (useTuple)
                tupleLocalBuilder = ilGenerator.DeclareLocal(unwrappedReturnTupe);
            var fallbackMode = sourceMethodInfo == null;
            if (fallbackMode) {
                if (!PrepareFallback(typeBuilder, wrapperMethodInfo, ctorInfos, ctorArgs, setting, ilGenerator, method, delegateInvoke, isStatic, baseMemberInfo)) {
                    ilGenerator.Emit(OpCodes.Ret);
                    return;
                }                    
            } else {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                Ldfld(ilGenerator, fieldInfo);
                TypeOf(ilGenerator, sourceType);
                TypeOf(ilGenerator, delegateType);
                ilGenerator.Emit(useTuple ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                var methodInfo = ReflectionHelperInterfaceWrapper.GetDelegateMethodInfo;
                if (genericParameters.Length > 0) {
                    ilGenerator.Emit(OpCodes.Ldc_I4, genericParameters.Length);
                    ilGenerator.Emit(OpCodes.Newarr, typeof(Type));
                    for (var i = 0; i < genericParameters.Length; i++) {
                        ilGenerator.Emit(OpCodes.Dup);
                        ilGenerator.Emit(OpCodes.Ldc_I4, i);
                        TypeOf(ilGenerator, genericParameterBuilders[i]);
                        ilGenerator.Emit(OpCodes.Stelem_Ref);
                    }
                    methodInfo = ReflectionHelperInterfaceWrapper.GetGenericDelegateMethodInfo;
                }
                EmitCall(ilGenerator,OpCodes.Call, methodInfo, null);
            }
            if (!isStatic) {
                Ldfld(ilGenerator, sourceObjectField);
            }
            for (byte i = 0; i < updatedParameterTypes.Length; i++) {
                var paramType = updatedParameterTypes[i];
                if (parameterTypes[i] != updatedParameterTypes[i]) {
                    ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                    if (paramType.IsByRef)
                        LSTind(ilGenerator, paramType.GetElementType(), false);
                    EmitCall(ilGenerator,OpCodes.Call, ReflectionHelperInterfaceWrapper.UnwrapMethodInfo, null);
                } else {
                    ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                    if (paramType.IsByRef)
                        LSTind(ilGenerator, paramType.GetElementType(), false);
                }                
            }
            EmitCall(ilGenerator,OpCodes.Call, delegateInvoke, null);

            if (useTuple) {
                SyncTupleItems(updatedParameterTypes.Select((x, i) => new Tuple<int, Type, Type>(i, x, parameterTypes[i])).Where(x => x.Item2.IsByRef),
                    unwrappedReturnTupe, wrapperMethodInfo.ReturnType != typeof(void), ilGenerator, tupleLocalBuilder, typeBuilder);
            }
            if (wrapReturnType) {
                TypeOf(ilGenerator, returnType);            
                EmitCall(ilGenerator,OpCodes.Call, ReflectionHelperInterfaceWrapper.WrapMethodInfo, null);
            }
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, wrapperMethodInfo);
        }        

        bool PrepareFallback(TypeBuilder typeBuilder, MethodInfo wrapperMethodInfo, List<FieldInfo> ctorInfos, List<object> ctorArgs, BaseReflectionHelperInterfaceWrapperSetting setting, ILGenerator ilGenerator, MemberInfoKind infoKind, MethodInfo delegateInvoke, bool isStatic, MemberInfo baseInfo) {
            var fallbackMode = setting.GetFallbackMode(wrapperMethodInfo, baseInfo, tWrapper);
            if (fallbackMode == ReflectionHelperFallbackMode.Default)
                fallbackMode = defaultFallbackMode;
            if(fallbackMode == ReflectionHelperFallbackMode.AbortWrapping)
                throw new MissingMemberException(String.Format("\r\nCannot bind the {0}.{1} with the source member\r\n", wrapperMethodInfo.DeclaringType, wrapperMethodInfo.Name));            
            var fallback = setting.GetFallback(infoKind);
            if (fallback == null) {
                if (fallbackMode != ReflectionHelperFallbackMode.ThrowNotImplementedException)
                    throw new ArgumentException(String.Format("\r\nCannot bind the {0}.{1} with the source member.\r\nPlease check spelling or define the fallback method with the following signature: \r\n\t{2}.\r\n", wrapperMethodInfo.DeclaringType, wrapperMethodInfo.Name, delegateInvoke.ToString()));
                if (!this.isStatic)
                    ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.ThrowException(typeof(NotImplementedException));
                return false;
            }
            var fallbackType = fallback.GetType();
            if (fallbackMode != ReflectionHelperFallbackMode.FallbackWithoutValidation) {                
                var fallbackInvoke = fallbackType.GetMethod("Invoke");
                var currentParameters = fallbackInvoke.GetParameters();
                var expectedParameters = delegateInvoke.GetParameters();
                StringBuilder exceptionBuilder = new StringBuilder();
                exceptionBuilder.AppendFormat("\r\nFallback method for the {0}.{1} has incorrect signature.\r\nExpected: {2};\r\nBut was: {3}.", wrapperMethodInfo.DeclaringType, wrapperMethodInfo.Name, delegateInvoke.ToString(), fallbackInvoke.ToString());
                if (currentParameters.Length != expectedParameters.Length || !fallbackInvoke.ReturnType.IsAssignableFrom(delegateInvoke.ReturnType)) {
                    throw new ArgumentException(exceptionBuilder.ToString() + "\r\n");
                }
                bool shouldThrow = false;
                for (int i = 0; i < currentParameters.Length; i++) {
                    var current = currentParameters[i];
                    var expected = expectedParameters[i];
                    if (current.ParameterType.IsAssignableFrom(expected.ParameterType))
                        continue;
                    exceptionBuilder.AppendFormat("\r\n\tParameter at {0}:\r\n\t\tShould be assignable with: {1}\r\n\t\tBut was: {2}", i, expected.ParameterType, current.ParameterType);
                    shouldThrow = true;
                }
                if (shouldThrow) {
                    throw new ArgumentException(exceptionBuilder.ToString() + "\r\n");
                }
            }
            var fallbackField = typeBuilder.DefineField("field" + wrapperMethodInfo.Name + "fallback",
                fallbackType, FieldAttributes.Private);
            ctorInfos.Add(fallbackField);
            ctorArgs.Add(fallback);
            Ldfld(ilGenerator, fallbackField);
            return true;
        }


        static void SyncTupleItems(IEnumerable<Tuple<int, Type, Type>> tuples, Type returnType, bool skipFirst,
            ILGenerator ilGenerator, LocalBuilder tupleLocalBuilder, TypeBuilder typeBuilder) {
            var index = skipFirst ? 1 : 0;
            ilGenerator.Emit(OpCodes.Stloc, tupleLocalBuilder);
            if (skipFirst) {
                ilGenerator.Emit(OpCodes.Ldloc, tupleLocalBuilder);
                EmitCall(ilGenerator,OpCodes.Call, GetTupleItem(returnType, 0), null);
            }
            var tpls = tuples.ToArray();
            for (var i = 0; i < tpls.Length; i++) {
                var tuple = tpls[i];
                var value = (byte)tuple.Item1 + 1;
                ilGenerator.Emit(OpCodes.Ldarg, value);                
                ilGenerator.Emit(OpCodes.Ldloc, tupleLocalBuilder);
                EmitCall(ilGenerator,OpCodes.Call, GetTupleItem(returnType, i + index), null);
                if (tuple.Item2 != tuple.Item3) {
                    TypeOf(ilGenerator, tuple.Item3.GetElementType());
                    EmitCall(ilGenerator,OpCodes.Call, ReflectionHelperInterfaceWrapper.WrapMethodInfo, null);
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
            return type.GetMethod(String.Format("get_Item{0}", i + 1));
        }

        bool GetIsInterface(BaseReflectionHelperInterfaceWrapperSetting setting, string name, MemberInfo memberInfo) {
            return setting.GetIsInterface(name, memberInfo);
        }
        static string GetTargetName(string wrapperMethodInfo, BaseReflectionHelperInterfaceWrapperSetting setting,
            MemberInfoKind kind, MemberInfo memberInfo) {
            var result = setting.GetName(wrapperMethodInfo, memberInfo);
            if (kind == MemberInfoKind.PropertyGetter && !result.Split('.').Last().StartsWith("get_"))
                return "get_" + result;
            if (kind == MemberInfoKind.PropertySetter && !result.Split('.').Last().StartsWith("set_"))
                return "set_" + result;
            return result;
        }

        BaseReflectionHelperInterfaceWrapperSetting GetSetting(MemberInfo wrapperMethodInfo, bool createNew = false) {
            ReflectionHelperInterfaceWrapperSetting result;
            if (settings.TryGetValue(wrapperMethodInfo, out result))
                return result;
            if (createNew) {
                result = new ReflectionHelperInterfaceWrapperSetting(this);
                settings[wrapperMethodInfo] = result;
                return result;
            }
            return new NullReflectionHelperInterfaceWrapperSetting(this);
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

        internal void WriteSetting(MemberInfo info, Action<ReflectionHelperInterfaceWrapperSetting> func) {
            var setting = (ReflectionHelperInterfaceWrapperSetting)GetSetting(info, true);
            func(setting);
        }
    }

    public class ReflectionHelperInstanceInterfaceWrapperGenerator<TWrapper> : ReflectionHelperInterfaceWrapperGenerator<TWrapper> {                
        public ReflectionHelperInstanceInterfaceWrapperGenerator(ModuleBuilder builder, object element) : base(builder, element, false) {}

        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, ReflectionHelperInstanceInterfaceWrapperGenerator<TWrapper>> DefineProperty(Expression<Func<TWrapper, object>> expression) {
            return DefineProperty<ReflectionHelperInstanceInterfaceWrapperGenerator<TWrapper>>(expression);
        }
        public InterfaceWrapperMemberInfoInstance<TWrapper, ReflectionHelperInstanceInterfaceWrapperGenerator<TWrapper>> DefineMethod(Expression<Action<TWrapper>> expression) {
            return DefineMethod<ReflectionHelperInstanceInterfaceWrapperGenerator<TWrapper>>(expression);
        }        
    }

    public class ReflectionHelperStaticInterfaceWrapperGenerator<TWrapper> : ReflectionHelperInterfaceWrapperGenerator<TWrapper> {
        public ReflectionHelperStaticInterfaceWrapperGenerator(ModuleBuilder builder, object element) : base(builder, element, true) {}
        public InterfaceWrapperPropertyMemberInfoInstance<TWrapper, ReflectionHelperStaticInterfaceWrapperGenerator<TWrapper>> DefineProperty(Expression<Func<TWrapper, object>> expression) {
            return DefineProperty<ReflectionHelperStaticInterfaceWrapperGenerator<TWrapper>>(expression);
        }

        public InterfaceWrapperMemberInfoInstance<TWrapper, ReflectionHelperStaticInterfaceWrapperGenerator<TWrapper>> DefineMethod(Expression<Action<TWrapper>> expression) {
            return DefineMethod<ReflectionHelperStaticInterfaceWrapperGenerator<TWrapper>>(expression);
        }
    }
    public class ReflectionHelperInterfaceWrapperGenerator<TWrapper> : BaseReflectionHelperInterfaceWrapperGenerator {        

        public ReflectionHelperInterfaceWrapperGenerator(ModuleBuilder builder, object element, bool isStatic) : base(builder, element, isStatic, typeof(TWrapper)) {            
        }

        public TWrapper Create() {
            return (TWrapper)CachedCreateImpl();
        }

        public ReflectionHelperInterfaceWrapperGenerator<TWrapper> DefaultBindingFlags(
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public) {
            defaultFlags = flags;
            return this;
        }

        public ReflectionHelperInterfaceWrapperGenerator<TWrapper> DefaultFallbackMode(ReflectionHelperFallbackMode mode) {
            defaultFallbackMode = mode;
            return this;
        }
        protected InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper> DefineProperty<TInterfaceWrapper>(
            Expression<Func<TWrapper, object>> expression) where TInterfaceWrapper : ReflectionHelperInterfaceWrapperGenerator<TWrapper> {
            if (expression.Body is MemberExpression)
                return
                    new InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper>(
                        (expression.Body as MemberExpression).Member,
                        (TInterfaceWrapper)this);
            if (expression.Body is UnaryExpression)
                return
                    new InterfaceWrapperPropertyMemberInfoInstance<TWrapper, TInterfaceWrapper>(
                        ((expression.Body as UnaryExpression).Operand as MemberExpression).Member,
                        (TInterfaceWrapper)this);
            return null;
        }

        protected InterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper> DefineMethod<TInterfaceWrapper>(
            Expression<Action<TWrapper>> expression) where TInterfaceWrapper : ReflectionHelperInterfaceWrapperGenerator<TWrapper> {
            return new InterfaceWrapperMemberInfoInstance<TWrapper, TInterfaceWrapper>((expression.Body as MethodCallExpression).Method,
                (TInterfaceWrapper)this);
        }        
    }    
}