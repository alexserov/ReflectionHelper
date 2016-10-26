using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ReflectionFramework;
using ReflectionFramework.Extensions;


namespace ReflectionHelperTests {
    internal class Class2 {
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
            var wrapped = cl2.Wrap<IClass2>();
            Boolean value = false;
            wrapped._Boolean(ref value);
            Assert.AreEqual(true, value);
        }

        [Test]
        public void _SByte() {
            //value = -10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            SByte value = 0;
            wrapped._SByte(ref value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _Byte() {
            //value = 10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            Byte value = 0;
            wrapped._Byte(ref value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Char() {
            //value = 'z';
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            Char value = '0';
            wrapped._Char(ref value);
            Assert.AreEqual('z', value);
        }

        [Test]
        public void _UInt16() {
            //value = 10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            UInt16 value = 0;
            wrapped._UInt16(ref value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int16() {
            //value = -10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            Int16 value = 0;
            wrapped._Int16(ref value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _UInt32() {
            //value = 10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            UInt32 value = 0;
            wrapped._UInt32(ref value);
            Assert.AreEqual(10u, value);
        }

        [Test]
        public void _Int32() {
            //value = -10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            Int32 value = 0;
            wrapped._Int32(ref value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _UInt64() {
            //value = 10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            UInt64 value = 0;
            wrapped._UInt64(ref value);
            Assert.AreEqual(10u, value);
        }

        [Test]
        public void _Int64() {
            //value = -10;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            Int64 value = 0;
            wrapped._Int64(ref value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _Single() {
            //value = 10f;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            Single value = 0;
            wrapped._Single(ref value);
            Assert.AreEqual(10f, value);
        }

        [Test]
        public void _Double() {
            //value = 10d;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            Double value = 0;
            wrapped._Double(ref value);
            Assert.AreEqual(10d, value);
        }

        [Test]
        public void _String() {
            //value = "z";
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            String value = "0";
            wrapped._String(ref value);
            Assert.AreEqual("z", value);
        }

        [Test]
        public void _DateTime() {
            //value = new DateTime(2000, 01, 01);
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            DateTime value = DateTime.Now;
            wrapped._DateTime(ref value);
            Assert.AreEqual(new DateTime(2000, 01, 01), value);
        }

        [Test]
        public void _Visibility() {
            //value = Visibility.Hidden;
            var cl2 = new Class2();
            var wrapped = cl2.Wrap<IClass2>();
            Visibility value = Visibility.Visible;
            wrapped._Visibility(ref value);
            Assert.AreEqual(Visibility.Hidden, value);
        }
    }
}
