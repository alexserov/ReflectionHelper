using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReflectionFramework;
using Xunit;


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
    public class RefAndReturnTests {
        [Fact]
        public void StringStringTest() {
            var cl3 = new Class3();
            var wrapped = cl3.Wrap<IClass3>();
            var str = "some";
            var result = wrapped.StringString(ref str);
            Assert.Equal("abc", result);
            Assert.Equal("def", str);
        }
        [Fact]
        public void StringStringVisibility() {
            var cl3 = new Class3();
            var wrapped = cl3.Wrap<IClass3>();
            var str = "some";
            var visib = Visibility.Collapsed;
            var result = wrapped.StringStringVisibility(ref str, ref visib);
            Assert.Equal("abc", result);
            Assert.Equal("def", str);
            Assert.Equal(Visibility.Hidden, visib);
        }
        [Fact]
        public void StringStringVisibilityOutRef() {
            var cl3 = new Class3();
            var wrapped = cl3.Wrap<IClass3>();
            var str = "some";
            var visib = Visibility.Collapsed;
            var result = wrapped.StringStringVisibilityOutRef(out str, ref visib);
            Assert.Equal("abc", result);
            Assert.Equal("def", str);
            Assert.Equal(Visibility.Hidden, visib);
        }
    }
}
