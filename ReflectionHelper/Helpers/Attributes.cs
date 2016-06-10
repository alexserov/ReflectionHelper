using System;

namespace DevExpress.Xpf.Core.Internal {
    public static class ReflectionHelperAttributes {
        [AttributeUsage(AttributeTargets.Property)]
        public class FieldAccessorAttribute : Attribute {}
    }
}