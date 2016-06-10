using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Xpf.Core.Internal;
using NUnit.Framework;

namespace WpfApplication5 {
    public class Class1 {
        private string publicStringProperty;
        public string LastMethod { get; set; }

        public string PublicStringProperty {
            get {
                LastMethod = "get_PublicStringProperty";
                return publicStringProperty;
            }
            set {
                LastMethod = "set_PublicStringProperty";
                publicStringProperty = value;
            }
        }

        public void PublicVoidMethod() {
            LastMethod = "PublicVoidMethod";
        }

        void PrivateVoidMethod() {
            LastMethod = "PrivateVoidMethod";
        }
    }

    public interface IClass1 {
        void PublicVoidMethod();
        void PrivateVoidMethod();
        string PublicStringProperty { get; set; }
    }

    [TestFixture]
    class Tests {
        [Test]
        public void PublicVoidMethodTest() {
            Class1 cl = new Class1();
            var wrapped = cl.Wrap2<IClass1>().Create();
            wrapped.PublicVoidMethod();
            Assert.AreEqual("PublicVoidMethod", cl.LastMethod);
        }
        [Test]
        public void PrivateVoidMethodTest() {
            Class1 cl = new Class1();
            var wrapped = cl.Wrap2<IClass1>()
                .DefineMember(x => x.PrivateVoidMethod())
                .BindingFlags(BindingFlags.NonPublic | BindingFlags.Instance)
                .EndMember()
                .Create();
            wrapped.PrivateVoidMethod();
            Assert.AreEqual("PrivateVoidMethod", cl.LastMethod);
        }
        [Test]
        public void PublicStringPropertyTest() {
            Class1 cl = new Class1();
            var wrapped = cl.Wrap2<IClass1>()
                .Create();
            wrapped.PublicStringProperty = "hello";
            Assert.AreEqual("set_PublicStringProperty", cl.LastMethod);
            Assert.AreEqual("hello", wrapped.PublicStringProperty);
            Assert.AreEqual("get_PublicStringProperty", cl.LastMethod);
        }
    }

    public class Class2 {
        public void _Boolean(ref Boolean value) {
            value = true;
        }

        public void _SByte(ref SByte value) {
            value = -10;
        }

        public void _Byte(ref Byte value) {
            value = 10;
        }

        public void _Char(ref Char value) {
            value = 'z';
        }

        public void _UInt16(ref UInt16 value) {
            value = 10;
        }

        public void _Int16(ref Int16 value) {
            value = -10;
        }

        public void _UInt32(ref UInt32 value) {
            value = 10;
        }

        public void _Int32(ref Int32 value) {
            value = -10;
        }

        public void _UInt64(ref UInt64 value) {
            value = 10;
        }

        public void _Int64(ref Int64 value) {
            value = -10;
        }

        public void _Single(ref Single value) {
            value = 10f;
        }

        public void _Double(ref Double value) {
            value = 10d;
        }

        public void _String(ref String value) {
            value = "z";
        }

        public void _DateTime(ref DateTime value) {
            value = new DateTime(2000, 01, 01);
        }
        public void _Visibility(ref Visibility value) {
            value = Visibility.Hidden;
        }
    }
    public interface IClass2 {
        void _Boolean(ref Boolean value);
        void _SByte(ref SByte value);
        void _Byte(ref Byte value);
        void _Char(ref Char value);
        void _UInt16(ref UInt16 value);
        void _Int16(ref Int16 value);
        void _UInt32(ref UInt32 value);
        void _Int32(ref Int32 value);
        void _UInt64(ref UInt64 value);
        void _Int64(ref Int64 value);
        void _Single(ref Single value);
        void _Double(ref Double value);
        void _String(ref String value);
        void _DateTime(ref DateTime value);
        void _Visibility(ref Visibility value);
    }

    [TestFixture]
    public class RefTests {
        [Test]
        public void _Boolean() {
            //value = true;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            Boolean value = false;
            wrapped._Boolean(ref value);
            Assert.AreEqual(true, value);
        }

        [Test]
        public void _SByte() {
            //value = -10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            SByte value = 0;
            wrapped._SByte(ref value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _Byte() {
            //value = 10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            Byte value = 0;
            wrapped._Byte(ref value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Char() {
            //value = 'z';
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            Char value = '0';
            wrapped._Char(ref value);
            Assert.AreEqual('z', value);
        }

        [Test]
        public void _UInt16() {
            //value = 10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            UInt16 value = 0;
            wrapped._UInt16(ref value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int16() {
            //value = -10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            Int16 value = 0;
            wrapped._Int16(ref value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _UInt32() {
            //value = 10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            UInt32 value = 0;
            wrapped._UInt32(ref value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int32() {
            //value = -10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            Int32 value = 0;
            wrapped._Int32(ref value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _UInt64() {
            //value = 10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            UInt64 value = 0;
            wrapped._UInt64(ref value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int64() {
            //value = -10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            Int64 value = 0;
            wrapped._Int64(ref value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _Single() {
            //value = 10f;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            Single value = 0;
            wrapped._Single(ref value);
            Assert.AreEqual(10f, value);
        }

        [Test]
        public void _Double() {
            //value = 10d;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            Double value = 0;
            wrapped._Double(ref value);
            Assert.AreEqual(10d, value);
        }

        [Test]
        public void _String() {
            //value = "z";
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            String value = "0";
            wrapped._String(ref value);
            Assert.AreEqual("z", value);
        }

        [Test]
        public void _DateTime() {
            //value = new DateTime(2000, 01, 01);
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            DateTime value = DateTime.Now;
            wrapped._DateTime(ref value);
            Assert.AreEqual(new DateTime(2000, 01, 01), value);
        }

        [Test]
        public void _Visibility() {
            //value = Visibility.Hidden;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap2<IClass2>().Create();
            Visibility value = Visibility.Visible;
            wrapped._Visibility(ref value);
            Assert.AreEqual(Visibility.Hidden, value);
        }
    }

    public class Class3 {
        public string StringString(ref string value) {
            value = "def";
            return "abc";
        }
        public string StringStringVisibility(ref string value1, ref Visibility value2) {
            value1 = "def";
            value2 = Visibility.Hidden;
            return "abc";
        }
        public string StringStringVisibilityOutRef(out string value1, ref Visibility value2) {
            value1 = "def";
            value2 = Visibility.Hidden;
            return "abc";
        }
    }

    public interface IClass3 {
        string StringString(ref string value);
        string StringStringVisibility(ref string value1, ref Visibility value2);
        string StringStringVisibilityOutRef(out string value1, ref Visibility value2);
    }
    [TestFixture]
    public class RefAndReturnTests {
        [Test]
        public void StringStringTest() {
            var cl3 = new Class3();
            var wrapped = cl3.Wrap2<IClass3>().Create();
            var str = "some";
            var result = wrapped.StringString(ref str);
            Assert.AreEqual("abc", result);
            Assert.AreEqual("def", str);
        }
        [Test]
        public void StringStringVisibility() {
            var cl3 = new Class3();
            var wrapped = cl3.Wrap2<IClass3>().Create();
            var str = "some";
            var visib = Visibility.Collapsed;
            var result = wrapped.StringStringVisibility(ref str, ref visib);
            Assert.AreEqual("abc", result);
            Assert.AreEqual("def", str);
            Assert.AreEqual(Visibility.Hidden, visib);
        }
        [Test]
        public void StringStringVisibilityOutRef() {
            var cl3 = new Class3();
            var wrapped = cl3.Wrap2<IClass3>().Create();
            var str = "some";
            var visib = Visibility.Collapsed;
            var result = wrapped.StringStringVisibilityOutRef(out str, ref visib);
            Assert.AreEqual("abc", result);
            Assert.AreEqual("def", str);
            Assert.AreEqual(Visibility.Hidden, visib);
        }
    }    

        public class Class4 {
        public void _Boolean(out Boolean value) {
            value = true;
        }

        public void _SByte(out SByte value) {
            value = -10;
        }

        public void _Byte(out Byte value) {
            value = 10;
        }

        public void _Char(out Char value) {
            value = 'z';
        }

        public void _UInt16(out UInt16 value) {
            value = 10;
        }

        public void _Int16(out Int16 value) {
            value = -10;
        }

        public void _UInt32(out UInt32 value) {
            value = 10;
        }

        public void _Int32(out Int32 value) {
            value = -10;
        }

        public void _UInt64(out UInt64 value) {
            value = 10;
        }

        public void _Int64(out Int64 value) {
            value = -10;
        }

        public void _Single(out Single value) {
            value = 10f;
        }

        public void _Double(out Double value) {
            value = 10d;
        }

        public void _String(out String value) {
            value = "z";
        }

        public void _DateTime(out DateTime value) {
            value = new DateTime(2000, 01, 01);
        }
        public void _Visibility(out Visibility value) {
            value = Visibility.Hidden;
        }
    }
    public interface IClass4 {
        void _Boolean(out Boolean value);
        void _SByte(out SByte value);
        void _Byte(out Byte value);
        void _Char(out Char value);
        void _UInt16(out UInt16 value);
        void _Int16(out Int16 value);
        void _UInt32(out UInt32 value);
        void _Int32(out Int32 value);
        void _UInt64(out UInt64 value);
        void _Int64(out Int64 value);
        void _Single(out Single value);
        void _Double(out Double value);
        void _String(out String value);
        void _DateTime(out DateTime value);
        void _Visibility(out Visibility value);
    }

    [TestFixture]
    public class OutTests {
        [Test]
        public void _Boolean() {
            //value = true;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            Boolean value = false;
            wrapped._Boolean(out value);
            Assert.AreEqual(true, value);
        }

        [Test]
        public void _SByte() {
            //value = -10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            SByte value = 0;
            wrapped._SByte(out value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _Byte() {
            //value = 10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            Byte value = 0;
            wrapped._Byte(out value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Char() {
            //value = 'z';
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            Char value = '0';
            wrapped._Char(out value);
            Assert.AreEqual('z', value);
        }

        [Test]
        public void _UInt16() {
            //value = 10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            UInt16 value = 0;
            wrapped._UInt16(out value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int16() {
            //value = -10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            Int16 value = 0;
            wrapped._Int16(out value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _UInt32() {
            //value = 10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            UInt32 value = 0;
            wrapped._UInt32(out value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int32() {
            //value = -10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            Int32 value = 0;
            wrapped._Int32(out value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _UInt64() {
            //value = 10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            UInt64 value = 0;
            wrapped._UInt64(out value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int64() {
            //value = -10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            Int64 value = 0;
            wrapped._Int64(out value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _Single() {
            //value = 10f;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            Single value = 0;
            wrapped._Single(out value);
            Assert.AreEqual(10f, value);
        }

        [Test]
        public void _Double() {
            //value = 10d;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            Double value = 0;
            wrapped._Double(out value);
            Assert.AreEqual(10d, value);
        }

        [Test]
        public void _String() {
            //value = "z";
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            String value = "0";
            wrapped._String(out value);
            Assert.AreEqual("z", value);
        }

        [Test]
        public void _DateTime() {
            //value = new DateTime(2000, 01, 01);
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            DateTime value = DateTime.Now;
            wrapped._DateTime(out value);
            Assert.AreEqual(new DateTime(2000, 01, 01), value);
        }

        [Test]
        public void _Visibility() {
            //value = Visibility.Hidden;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap2<IClass4>().Create();
            Visibility value = Visibility.Visible;
            wrapped._Visibility(out value);
            Assert.AreEqual(Visibility.Hidden, value);
        }
    }

    public class Class5 {
        public string String { get; set; }
        public void OptionalParamsMethod1(string str = "hello") {
            String = str;
        }
        public void OptionalParamsMethod2(string str = "hello") {
            String = str;
        }
        public void OptionalParamsMethod3(string str) {
            String = str;
        }
    }

    public interface IClass5 {
        void OptionalParamsMethod1(string str = "hello");
        void OptionalParamsMethod2(string str);
        void OptionalParamsMethod3(string str = "hello");
    }

    [TestFixture]
    public class OptionalParametersTest {
        [Test]
        public void OptionalParamsMethod1() {
            var cl5 = new Class5();
            var wcl5 = cl5.Wrap2<IClass5>().Create();
            wcl5.OptionalParamsMethod1();
            Assert.AreEqual("hello", cl5.String);
            wcl5.OptionalParamsMethod1("world");
            Assert.AreEqual("world", cl5.String);
        }

        [Test]
        public void OptionalParamsMethod2() {
            var cl5 = new Class5();
            var wcl5 = cl5.Wrap2<IClass5>().Create();
            wcl5.OptionalParamsMethod2("hello");
            Assert.AreEqual("hello", cl5.String);            
        }
        [Test]
        public void OptionalParamsMethod3() {
            var cl5 = new Class5();
            var wcl5 = cl5.Wrap2<IClass5>().Create();
            wcl5.OptionalParamsMethod3();
            Assert.AreEqual("hello", cl5.String);
            wcl5.OptionalParamsMethod3("world");
            Assert.AreEqual("world", cl5.String);
        }
    }

    public class Class6 {
        public Type GenericMethod<TArgument>() {
            return typeof(TArgument);
        }
        public Type[] GenericMethod2<TArgument, TArgument2>() {
            return new[] {typeof(TArgument), typeof(TArgument2)};
        }
        public TArgument GenericMethod3<TArgument>(TArgument value) {
            return value;
        }

        public class FakeClass {
            public int Value { get; set; }
        }

        public TArgument GenericMethodRef<TArgument>(ref TArgument value) where TArgument : FakeClass {
            var result = new FakeClass() {Value = value.Value};
            value.Value++;
            return (TArgument) result;
        }
    }

    public interface IClass6 {
        Type GenericMethod<TArgument>();
        Type[] GenericMethod2<TArgument, TArgument2>();
        TArgument GenericMethod3<TArgument>(TArgument value);
        TArgument GenericMethodRef<TArgument>(ref TArgument value);
    }

    [TestFixture]
    public class GenericTests {
        [Test]
        public void SimpleTest() {
            var c6 = new Class6();
            var ic6 = c6.Wrap2<IClass6>().Create();
            Assert.AreEqual(typeof(string), ic6.GenericMethod<string>());
            Assert.AreEqual(typeof(int), ic6.GenericMethod<int>());
        }

        [Test]
        public void SimpleTest2() {
            var c6 = new Class6();
            var ic6 = c6.Wrap2<IClass6>().Create();
            var types = ic6.GenericMethod2<string, Visibility>();
            Assert.AreEqual(typeof(string), types[0]);
            Assert.AreEqual(typeof(Visibility), types[1]);
        }
        [Test]
        public void SimpleTest3() {
            var c6 = new Class6();
            var ic6 = c6.Wrap2<IClass6>().Create();
            Assert.AreEqual("hello", ic6.GenericMethod3<string>("hello"));
        }
        [Test]
        public void RefTest() {
            var c6 = new Class6();
            Class6.FakeClass fake = new Class6.FakeClass() {Value = 10};
            var ic6 = c6.Wrap2<IClass6>().Create();
            var result = ic6.GenericMethodRef(ref fake);
            Assert.AreEqual(10, result.Value);
            Assert.AreEqual(11, fake.Value);
        }
    }
}
