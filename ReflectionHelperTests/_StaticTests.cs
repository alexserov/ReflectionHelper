﻿using System.Reflection;
using DevExpress.Xpf.Core.Internal;
using NUnit.Framework;

namespace ReflectionHelperTests {
    internal class StaticFulentAPITestObject {
        private static string stringField;
        public static string PublicStringProperty { get; set; }
        public static string PrivateStringProperty { get; set; }

        public static string PublicMethod(string value) {
            return value;
        }

        private static string PrivateMethod(string value) {
            return value;
        }
    }

    public interface IStaticFluentAPITestObject {
        string stringField { get; set; }
        string PublicStringProperty { get; set; }
        string PrivateStringProperty { get; set; }

        string PublicMethod(string value);
        string PrivateMethod(string value);
    }

    [TestFixture]
    public class StaticFluentAPITests {
        [Test]
        public void Simple() {
            var ti = ReflectionGenerator.DefineWrapper<StaticFulentAPITestObject, IFluentAPITestObject>()
                .DefineProperty(x => x.PrivateStringProperty)
                .BindingFlags(BindingFlags.NonPublic)
                .EndMember()
                .DefineProperty(x => x.stringField)
                .BindingFlags(BindingFlags.NonPublic)
                .FieldAccessor()
                .EndMember()
                .DefineMethod(x => x.PrivateMethod(null))
                .BindingFlags(BindingFlags.NonPublic | BindingFlags.Public)
                .EndMember()
                .Create();
            Assert.AreEqual("hello", ti.PublicMethod("hello"));
            ti.stringField = "hello";
            Assert.AreEqual("hello", ti.stringField);
        }
    }
}