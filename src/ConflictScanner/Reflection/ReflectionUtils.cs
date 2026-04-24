using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace ConflictScanner.Reflection
{
    public static class ReflectionUtils
    {
        /// <summary>
        /// Loads an assembly into a collectible context for reflection-only analysis.
        /// Returns null if loading fails.
        /// </summary>
        public static Assembly? LoadAssemblySafe(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                var alc = new AssemblyLoadContext($"scan-{Path.GetFileNameWithoutExtension(path)}", isCollectible: true);
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
