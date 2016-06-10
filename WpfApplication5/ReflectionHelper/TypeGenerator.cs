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
                DefineMethod(typeBuilder, wrapperMethodInfo, ctorInfos, ctorArgs, sourceType,
                    sourceObjectField, GetSetting(wrapperMethodInfo), MemberInfoKind.Method);
            }
            foreach (PropertyInfo propertyInfo in typeof(TWrapper).GetProperties()) {
                var setting = GetSetting(propertyInfo);
                if (propertyInfo.GetMethod != null)
                    DefineMethod(typeBuilder, propertyInfo.GetMethod, ctorInfos, ctorArgs, sourceType,
                        sourceObjectField, setting, MemberInfoKind.PropertyGetter);
                if (propertyInfo.SetMethod != null)
                    DefineMethod(typeBuilder, propertyInfo.SetMethod, ctorInfos, ctorArgs, sourceType,
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

        private static void DefineMethod(TypeBuilder typeBuilder, MethodInfo wrapperMethodInfo,
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