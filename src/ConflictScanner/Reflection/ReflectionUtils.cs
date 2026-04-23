using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace ConflictScanner.Reflection
{
    /// <summary>
    /// Utility helpers for safe reflection loading.
    /// This will eventually provide:
    /// - AssemblyLoadContext sandboxing
    /// - Safe loading without executing static constructors
    /// - Method scanning helpers
    /// - Attribute scanning helpers
    /// </summary>
    public static class ReflectionUtils
    {
        /// <summary>
        /// Loads an assembly in an isolated context.
        /// Static constructors are NOT executed.
        /// </summary>
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
