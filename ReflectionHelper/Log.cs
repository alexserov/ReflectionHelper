using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ReflectionHelper {
    public static class Log {
        public static bool Enabled = false;
        public static void Write(string text) {
            if (!Enabled)
                return;
            using (var writer = new StreamWriter("log.txt", true)) {
                writer.WriteLine(text);
            }
        }        
    }
}
