using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ReflectionFramework;



namespace ReflectionHelperTests {
    internal class Class6 {
        public Type GenericMethod<TArgument>() {
            return typeof(TArgument);
        }
        public Type[] GenericMethod2<TArgument, TArgument2>() {
            return new[] { typeof(TArgument), typeof(TArgument2) };
        }
        public TArgument GenericMethod3<TArgument>(TArgument value) {
            return value;
        }

        public class FakeClass {
            public int Value { get; set; }
        }

        public TArgument GenericMethodRef<TArgument>(ref TArgument value) where TArgument : FakeClass {
            var result = new FakeClass() { Value = value.Value };
            value.Value++;
            return (TArgument)result;
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
            var ic6 = c6.Wrap<IClass6>();
            Assert.AreEqual(typeof(string), ic6.GenericMethod<string>());
            Assert.AreEqual(typeof(int), ic6.GenericMethod<int>());
        }

        [Test]
        public void SimpleTest2() {
            var c6 = new Class6();
            var ic6 = c6.Wrap<IClass6>();
            var types = ic6.GenericMethod2<string, Visibility>();
            Assert.AreEqual(typeof(string), types[0]);
            Assert.AreEqual(typeof(Visibility), types[1]);
        }
        [Test]
        public void SimpleTest3() {
            var c6 = new Class6();
            var ic6 = c6.Wrap<IClass6>();
            Assert.AreEqual("hello", ic6.GenericMethod3<string>("hello"));
        }
        [Test]
        public void RefTest() {
            var c6 = new Class6();
            Class6.FakeClass fake = new Class6.FakeClass() { Value = 10 };
            var ic6 = c6.Wrap<IClass6>();
            var result = ic6.GenericMethodRef(ref fake);
            Assert.AreEqual(10, result.Value);
            Assert.AreEqual(11, fake.Value);
        }
    }
}
