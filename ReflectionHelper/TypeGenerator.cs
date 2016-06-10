using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace DevExpress.Xpf.Core.Internal {
    public class BaseReflectionGeneratorInstance {
        protected internal BindingFlags defaultFlags = BindingFlags.Instance | BindingFlags.Public;
    }

    public class ReflectionGeneratorInstance<TWrapper> : BaseReflectionGeneratorInstance {
        private readonly object element;
        private readonly Type elementType;
        private readonly bool isStatic;
        private readonly ModuleBuilder moduleBuilder;
        private readonly Dictionary<MemberInfo, ReflectionGeneratorInstanceSetting> settings;

        public ReflectionGeneratorInstance(ModuleBuilder builder, object element, bool isStatic) {
            if (isStatic) {
                this.element = null;
                elementType = (Type) element;
            }
            else {
                this.element = element;
                elementType = this.element?.GetType();
            }
            moduleBuilder = builder;
            this.isStatic = isStatic;
            settings = new Dictionary<MemberInfo, ReflectionGeneratorInstanceSetting>();
        }

        public ReflectionGeneratorInstance<TWrapper> DefaultBindingFlags(
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public) {
            defaultFlags = flags;
            return this;
        }

        public ReflectionGeneratorPropertyMemberInfoInstance<TWrapper> DefineProperty(
            Expression<Func<TWrapper, object>> expression) {
            return
                new ReflectionGeneratorPropertyMemberInfoInstance<TWrapper>(
                    (expression.Body as MemberExpression).Member,
                    this);
        }

        public ReflectionGeneratorMemberInfoInstance<TWrapper> DefineMethod(
            Expression<Action<TWrapper>> expression) {
            return new ReflectionGeneratorMemberInfoInstance<TWrapper>((expression.Body as MethodCallExpression).Method,
                this);
        }

        public TWrapper Create() {
            var typeBuilder = moduleBuilder.DefineType(typeof(TWrapper).Name + Guid.NewGuid(),
                TypeAttributes.Public,
                typeof(ReflectionGeneratedObject));

            typeBuilder.AddInterfaceImplementation(typeof(TWrapper));
            var sourceType = elementType;
            var ctorArgs = new List<object>();
            var ctorInfos = new List<FieldInfo>();
            var sourceObjectField = typeBuilder.DefineField("fieldSourceObject", sourceType, FieldAttributes.Family);
            ctorInfos.Add(sourceObjectField);
            ctorArgs.Add(element);

            foreach (var wrapperMethodInfo in typeof(TWrapper).GetMethods()) {
                if (wrapperMethodInfo.IsSpecialName)
                    continue;
                DefineMethod(typeBuilder, wrapperMethodInfo, ctorInfos, ctorArgs, sourceType,
                    sourceObjectField, GetSetting(wrapperMethodInfo), MemberInfoKind.Method, isStatic);
            }
            foreach (var propertyInfo in typeof(TWrapper).GetProperties()) {
                var setting = GetSetting(propertyInfo);
                var field = setting.FieldAccessor();
                var getMethod = propertyInfo.GetGetMethod(true);
                var setMethod = propertyInfo.GetSetMethod(true);
                if (getMethod != null)
                    if (field)
                        DefineFieldGetterOrSetter(typeBuilder, getMethod, ctorInfos, ctorArgs, sourceType,
                            sourceObjectField, setting, MemberInfoKind.PropertyGetter, isStatic);
                    else
                        DefineMethod(typeBuilder, getMethod, ctorInfos, ctorArgs, sourceType,
                            sourceObjectField, setting, MemberInfoKind.PropertyGetter, isStatic);
                if (setMethod != null)
                    if (field)
                        DefineFieldGetterOrSetter(typeBuilder, setMethod, ctorInfos, ctorArgs, sourceType,
                            sourceObjectField, setting, MemberInfoKind.PropertySetter, isStatic);
                    else
                        DefineMethod(typeBuilder, setMethod, ctorInfos, ctorArgs, sourceType,
                            sourceObjectField, setting, MemberInfoKind.PropertySetter, isStatic);
            }
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                ctorInfos.Select(x => x.FieldType).ToArray());
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

        private static void DefineFieldGetterOrSetter(TypeBuilder typeBuilder, MethodInfo wrapperMethodInfo,
            List<FieldInfo> ctorInfos,
            List<object> ctorArgs, Type sourceType, FieldBuilder sourceObjectField,
            BaseReflectionGeneratorInstanceSetting setting, MemberInfoKind method, bool isStatic) {
            var sourceFieldInfo =
                sourceType.GetField(GetTargetName(wrapperMethodInfo.Name.Remove(0, 4), setting, MemberInfoKind.Method),
                    setting.GetBindingFlags() | (isStatic ? BindingFlags.Static : 0));
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
            if (sourceFieldInfo == null) {
                DoFallback(typeBuilder, wrapperMethodInfo, ctorInfos, ctorArgs, setting, ilGenerator, methodBuilder);
                return;
            }
            var returnType = wrapperMethodInfo.ReturnType;
            var useTuple = false;
            var delegateType = ReflectionHelper.MakeGenericDelegate(parameterTypes, ref returnType,
                isStatic ? null : typeof(object), out useTuple);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fieldInfo);
            ilGenerator.Emit(OpCodes.Ldtoken, delegateType);
            ilGenerator.Emit(OpCodes.Ldtoken, typeof(object));
            ilGenerator.Emit(OpCodes.Ldtoken, sourceFieldInfo.FieldType);
            if (isStatic)
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
            else
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.EmitCall(OpCodes.Call,
                method == MemberInfoKind.PropertyGetter
                    ? ReflectionGeneratedObject.GetFieldGetterMethodInfo
                    : ReflectionGeneratedObject.GetFieldSetterMethodInfo, null);
            ReflectionHelper.CastClass(ilGenerator, typeof(Delegate), delegateType);
            if (!isStatic) {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, sourceObjectField);
            }
            for (byte i = 0; i < parameterTypes.Length; i++) {
                ilGenerator.Emit(OpCodes.Ldarg, i + 1);
            }
            ilGenerator.EmitCall(OpCodes.Call, delegateType.GetMethod("Invoke"), null);
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, typeof(TWrapper).GetMethod(wrapperMethodInfo.Name));
        }

        private static void DefineMethod(TypeBuilder typeBuilder, MethodInfo wrapperMethodInfo,
            List<FieldInfo> ctorInfos,
            List<object> ctorArgs, Type sourceType, FieldBuilder sourceObjectField,
            BaseReflectionGeneratorInstanceSetting setting, MemberInfoKind method, bool isStatic) {
            var sourceMethodInfo = sourceType.GetMethod(GetTargetName(wrapperMethodInfo.Name, setting, method),
                setting.GetBindingFlags() | (isStatic ? BindingFlags.Static : 0));
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
            var ilGenerator = methodBuilder.GetILGenerator();
            if (sourceMethodInfo == null) {
                DoFallback(typeBuilder, wrapperMethodInfo, ctorInfos, ctorArgs, setting, ilGenerator, methodBuilder);
                return;
            }
            var returnType = wrapperMethodInfo.ReturnType;
            var useTuple = false;
            var delegateType = ReflectionHelper.MakeGenericDelegate(parameterTypes, ref returnType,
                isStatic ? null : typeof(object), out useTuple);
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
            ReflectionHelper.CastClass(ilGenerator, typeof(Delegate), delegateType);
            if (!isStatic) {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, sourceObjectField);
            }
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

        private static void DoFallback(TypeBuilder typeBuilder, MethodInfo wrapperMethodInfo, List<FieldInfo> ctorInfos, List<object> ctorArgs,
            BaseReflectionGeneratorInstanceSetting setting, ILGenerator ilGenerator, MethodBuilder methodBuilder) {
            var fallbackField = typeBuilder.DefineField("field" + wrapperMethodInfo.Name + "fallback",
                typeof(Action), FieldAttributes.Family);
            ctorInfos.Add(fallbackField);
            ctorArgs.Add(setting.GetFallback());
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fallbackField);
            ilGenerator.EmitCall(OpCodes.Call, typeof(Action).GetMethod("Invoke"), null);
            ilGenerator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(methodBuilder, typeof(TWrapper).GetMethod(wrapperMethodInfo.Name));
        }

        private static void SyncTupleItems(IEnumerable<Tuple<int, Type>> tuples, Type returnType, bool skipFirst,
            ILGenerator ilGenerator) {
            var index = skipFirst ? 1 : 0;
            ilGenerator.Emit(OpCodes.Stloc_0);
            if (skipFirst) {
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.EmitCall(OpCodes.Call, GetTupleItem(returnType, 0), null);
            }
            var tpls = tuples.ToArray();
            for (var i = 0; i < tpls.Length; i++) {
                var tuple = tpls[i];
                var value = (byte) tuple.Item1 + 1;
                ilGenerator.Emit(OpCodes.Ldarg, value);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.EmitCall(OpCodes.Call, GetTupleItem(returnType, i + index), null);
                LSTind(ilGenerator, tuple.Item2.GetElementType(), true);
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
                    }
                    else {
                        generator.Emit(stind ? OpCodes.Stobj : OpCodes.Ldobj, type);
                        return;
                    }
                    break;
            }
            generator.Emit(opCode);
        }

        private static MethodInfo GetTupleItem(Type type, int i) {
            return type.GetMethod($"get_Item{i + 1}");
        }

        private static string GetTargetName(string wrapperMethodInfo, BaseReflectionGeneratorInstanceSetting setting,
            MemberInfoKind kind) {
            var result = setting.GetName(wrapperMethodInfo);
            if (kind == MemberInfoKind.PropertyGetter && !result.StartsWith("get_"))
                return "get_" + result;
            if (kind == MemberInfoKind.PropertySetter && !result.StartsWith("set_"))
                return "set_" + result;
            return wrapperMethodInfo;
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

        internal void WriteSetting(MemberInfo info, Action<ReflectionGeneratorInstanceSetting> func) {
            var setting = (ReflectionGeneratorInstanceSetting) GetSetting(info, true);
            func(setting);
        }

        private enum MemberInfoKind {
            Method,
            PropertyGetter,
            PropertySetter
        }
    }

    public static class ReflectionGenerator {
        private const string typesAssemblyName = "reflectiongeneratortypes";
        private const string typesModuleName = "reflectiongeneratormodule";
        internal static ModuleBuilder moduleBuilder;
        private static readonly AssemblyBuilder assemblyBuilder;

        static ReflectionGenerator() {
            var asmName = new AssemblyName(typesAssemblyName);
            assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(typesModuleName, typesAssemblyName + ".dll");
        }

        public static ReflectionGeneratorInstance<TWrapper> DefineWrapper<TType, TWrapper>() {
            return typeof(TType).DefineWrapper<TWrapper>();
        }

        public static ReflectionGeneratorInstance<TWrapper> DefineWrapper<TWrapper>(this object element) {
            return new ReflectionGeneratorInstance<TWrapper>(moduleBuilder, element, false);
        }

        public static ReflectionGeneratorInstance<TWrapper> DefineWrapper<TWrapper>(this Type element) {
            return new ReflectionGeneratorInstance<TWrapper>(moduleBuilder, element, true);
        }

        public static TWrapper Wrap<TWrapper>(this object element) {
            return element.DefineWrapper<TWrapper>().Create();
        }

        public static TWrapper Wrap<TWrapper>(this Type targetType) {
            return targetType.DefineWrapper<TWrapper>().Create();
        }

        public static TWrapper Wrap<TType, TWrapper>() {
            return typeof(TType).Wrap<TWrapper>();
        }

        public static void Save() {
            assemblyBuilder.Save($"myasm{DateTime.Now.Minute}.dll");
        }
    }
}