using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace ConflictScanner.Reflection
{
    public static class ReflectionUtils
    {
        public static Assembly LoadAssemblySafe(string path)
        {
            try
            {
                var alc = new AssemblyLoadContext("ScannerContext", isCollectible: true);
                using var stream = File.OpenRead(path);
                return alc.LoadFromStream(stream);
            }
            catch
            {
                return null;
            }
        }
    }
}
