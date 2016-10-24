using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ReflectionHelperTests
{
    [TestFixture]
    public class _APITests
    {
        [Test]
        [Explicit]
        public void Test() {
            var asm = typeof(ReflectionFramework.ReflectionHelper).Assembly;
            StringBuilder builder = new StringBuilder();
            foreach (var exportedType in asm.GetExportedTypes()) {
                if(exportedType.Namespace.Contains("Internal"))
                    continue;                
                builder.AppendLine(exportedType.FullName);                
                foreach (var memberInfo in exportedType.GetMembers()) {
                    if(memberInfo.DeclaringType == typeof(object))
                        continue;
                    if(memberInfo is MethodInfo && ((MethodInfo)memberInfo).IsSpecialName || memberInfo is ConstructorInfo)
                        continue;                                        
                    builder.AppendLine($"\t{memberInfo.Name} ({memberInfo.GetType()}");
                }
            }
            Debugger.Break();
        }
    }
}
