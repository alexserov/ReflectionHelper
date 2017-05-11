using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

#if RHELPER
using ReflectionFramework;
using ReflectionFramework.Attributes;
using ReflectionFramework.Extensions;

namespace ReflectionHelperTests
#else
using DevExpress.Xpf.Core.ReflectionExtensions.Attributes;
using DevExpress.Xpf.Core.ReflectionExtensions;
using DevExpress.Xpf.Core.Internal;

namespace DevExpress.Xpf.Core.ReflectionExtensions.Tests
#endif
{
    [TestFixture]
    public class _APITests
    {
        [Test]
        [Explicit]
        public void Test() {
            var asm = typeof(ReflectionHelper).Assembly;
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
