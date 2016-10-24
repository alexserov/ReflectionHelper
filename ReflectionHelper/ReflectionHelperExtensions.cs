using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using ReflectionFramework.Internal;

namespace ReflectionFramework {
    public static class ReflectionHelperExtensions {
        const string typesAssemblyName = "ReflectionHelperTypes";
        const string typesModuleName = "ReflectionHelperModule";
        static ModuleBuilder moduleBuilder;
        static readonly AssemblyBuilder assemblyBuilder;
        //public const string DynamicAssemblyName = @"ReflectionHelperTypes, PublicKeyToken=6d662ee35c32032e";
        public const string DynamicAssemblyName = @"ReflectionHelperTypes, PublicKey=0024000004800000940000000602000000240000525341310004000001000100758f95f5e23e5e7f8191599ba1e7262093b5c8ca5d329e360a8d61f3b94f4ac23315703141ecf151723fbfcf6a1e7f09f2c3f3b824068293216482b4324596729e7d2973e73660aa11d59c2bd36d66b8799faa6802d29382d38cfbd9634c10424d1bfd43854769fe875bdd194a5527b45b61a2bbebb84d70180ca486748901f6";
        static ReflectionHelperExtensions() {
            var asmName = new AssemblyName(typesAssemblyName);
            asmName.KeyPair = new StrongNameKeyPair(ReflectionHelperAssemblyKey.strongKeyPair);
            assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(typesModuleName);
        }

        public static ReflectionHelperStaticInterfaceWrapperGenerator<TWrapper> DefineWrapper<TType, TWrapper>() {
            return typeof(TType).DefineWrapper<TWrapper>();
        }

        public static ReflectionHelperInstanceInterfaceWrapperGenerator<TWrapper> DefineWrapper<TWrapper>(this object element) {
            return new ReflectionHelperInstanceInterfaceWrapperGenerator<TWrapper>(moduleBuilder, element);
        }

        public static ReflectionHelperStaticInterfaceWrapperGenerator<TWrapper> DefineWrapper<TWrapper>(this Type element) {
            return new ReflectionHelperStaticInterfaceWrapperGenerator<TWrapper>(moduleBuilder, element);
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
        internal static object Wrap(object element, Type wrapperType) {
            return new BaseReflectionHelperInterfaceWrapperGenerator(moduleBuilder, element, false, wrapperType).CreateInternal();
        }
    }
}