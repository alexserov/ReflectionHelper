using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using ReflectionFramework.Internal;

namespace ReflectionFramework {
    public static class ReflectionGenerator {
        const string typesAssemblyName = "reflectiongeneratortypes";
        const string typesModuleName = "reflectiongeneratormodule";
        internal static ModuleBuilder moduleBuilder;
        static readonly AssemblyBuilder assemblyBuilder;

        static ReflectionGenerator() {
            var asmName = new AssemblyName(typesAssemblyName);
            assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(typesModuleName, typesAssemblyName + ".dll");
        }

        public static ReflectionGeneratorStaticWrapper<TWrapper> DefineWrapper<TType, TWrapper>() {
            return typeof(TType).DefineWrapper<TWrapper>();
        }

        public static ReflectionGeneratorInstanceWrapper<TWrapper> DefineWrapper<TWrapper>(this object element) {
            return new ReflectionGeneratorInstanceWrapper<TWrapper>(moduleBuilder, element);
        }

        public static ReflectionGeneratorStaticWrapper<TWrapper> DefineWrapper<TWrapper>(this Type element) {
            return new ReflectionGeneratorStaticWrapper<TWrapper>(moduleBuilder, element);
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