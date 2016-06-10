using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Xpf.Core.Internal;
using NUnit.Framework;

namespace ReflectionHelperTests {
    class Class4 {
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
            var wrapped = cl2.Wrap<IClass4>();
            Boolean value = false;
            wrapped._Boolean(out value);
            Assert.AreEqual(true, value);
        }

        [Test]
        public void _SByte() {
            //value = -10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            SByte value = 0;
            wrapped._SByte(out value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _Byte() {
            //value = 10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            Byte value = 0;
            wrapped._Byte(out value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Char() {
            //value = 'z';
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            Char value = '0';
            wrapped._Char(out value);
            Assert.AreEqual('z', value);
        }

        [Test]
        public void _UInt16() {
            //value = 10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            UInt16 value = 0;
            wrapped._UInt16(out value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int16() {
            //value = -10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            Int16 value = 0;
            wrapped._Int16(out value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _UInt32() {
            //value = 10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            UInt32 value = 0;
            wrapped._UInt32(out value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int32() {
            //value = -10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            Int32 value = 0;
            wrapped._Int32(out value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _UInt64() {
            //value = 10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            UInt64 value = 0;
            wrapped._UInt64(out value);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void _Int64() {
            //value = -10;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            Int64 value = 0;
            wrapped._Int64(out value);
            Assert.AreEqual(-10, value);
        }

        [Test]
        public void _Single() {
            //value = 10f;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            Single value = 0;
            wrapped._Single(out value);
            Assert.AreEqual(10f, value);
        }

        [Test]
        public void _Double() {
            //value = 10d;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            Double value = 0;
            wrapped._Double(out value);
            Assert.AreEqual(10d, value);
        }

        [Test]
        public void _String() {
            //value = "z";
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            String value = "0";
            wrapped._String(out value);
            Assert.AreEqual("z", value);
        }

        [Test]
        public void _DateTime() {
            //value = new DateTime(2000, 01, 01);
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            DateTime value = DateTime.Now;
            wrapped._DateTime(out value);
            Assert.AreEqual(new DateTime(2000, 01, 01), value);
        }

        [Test]
        public void _Visibility() {
            //value = Visibility.Hidden;
            var cl2 = new Class4();
            var wrapped = cl2.Wrap<IClass4>();
            Visibility value = Visibility.Visible;
            wrapped._Visibility(out value);
            Assert.AreEqual(Visibility.Hidden, value);
        }
    }
}
