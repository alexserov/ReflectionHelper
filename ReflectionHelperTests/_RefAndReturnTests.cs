using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ReflectionFramework;



namespace ReflectionHelperTests {
    internal class Class3 {
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
            var wrapped = cl3.Wrap<IClass3>();
            var str = "some";
            var result = wrapped.StringString(ref str);
            Assert.AreEqual("abc", result);
            Assert.AreEqual("def", str);
        }
        [Test]
        public void StringStringVisibility() {
            var cl3 = new Class3();
            var wrapped = cl3.Wrap<IClass3>();
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
            var wrapped = cl3.Wrap<IClass3>();
            var str = "some";
            var visib = Visibility.Collapsed;
            var result = wrapped.StringStringVisibilityOutRef(out str, ref visib);
            Assert.AreEqual("abc", result);
            Assert.AreEqual("def", str);
            Assert.AreEqual(Visibility.Hidden, visib);
        }
    }
}
