using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConflictScanner.Reflection
{
    /// <summary>
    /// Reflection-based Nautilus analysis for Deep Scan mode.
    /// Extracts TechType IDs, CraftTree paths, and Sprite keys from Nautilus API calls.
    /// </summary>
    public class NautilusReflectionAnalyzer : IAnalyzer
    {
        private readonly Dictionary<string, List<string>> techTypeMap =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<string>> craftTreeMap =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<string>> spriteMap =
            new(StringComparer.OrdinalIgnoreCase);

        public void Run(ScanContext context)
        {
            if (context.Mode == ScanMode.Quick)
                return;

            techTypeMap.Clear();
            craftTreeMap.Clear();
            spriteMap.Clear();

            string bepPath = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (!Directory.Exists(bepPath))
                return;

            foreach (var modFolder in Directory.GetDirectories(bepPath))
            {
                string modName = Path.GetFileName(modFolder);

                foreach (var dll in Directory.GetFiles(modFolder, "*.dll", SearchOption.AllDirectories))
                {
                    AnalyzeAssembly(dll, modName, context);
                }
            }

            ReportConflicts(context);
        }

        private void AnalyzeAssembly(string dllPath, string modName, ScanContext context)
        {
            Assembly? asm = ReflectionUtils.LoadAssemblySafe(dllPath);
            if (asm == null)
            {
                context.AddNautilusWarning(
                    Severity.Info,
                    $"[{modName}] Failed to load assembly for reflection: {Path.GetFileName(dllPath)}"
                );
                return;
            }

            IEnumerable<Type> types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null)!;
            }
            catch (Exception ex)
            {
                context.AddNautilusWarning(
                    Severity.Info,
                    $"[{modName}] Could not inspect types in {Path.GetFileName(dllPath)}: {ex.Message}"
                );
                return;
            }

            foreach (var type in types)
            {
                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Static |
                        BindingFlags.Instance);
                }
                catch
                {
                    continue;
                }

                foreach (var method in methods)
                {
                    try
                    {
                        AnalyzeMethod(method, modName);
                    }
                    catch
                    {
                        // Resilient against individual method parse failures
                    }
                }
            }
        }

        private void AnalyzeMethod(MethodInfo method, string modName)
        {
            foreach (var (target, args) in ILReader.FindCalls(method))
            {
                if (NautilusSignatures.IsTechTypeRegistration(target))
                {
                    string? id = args.FirstOrDefault() as string;
                    if (!string.IsNullOrWhiteSpace(id) && id != "(unknown)")
                        Register(techTypeMap, id, modName);
                }

                if (NautilusSignatures.IsCraftTreeRegistration(target))
                {
                    string? path = args.FirstOrDefault() as string;
                    if (!string.IsNullOrWhiteSpace(path) && path != "(unknown)")
                        Register(craftTreeMap, path, modName);
                }

                if (NautilusSignatures.IsSpriteRegistration(target))
                {
                    string? key = args.FirstOrDefault() as string;
                    if (!string.IsNullOrWhiteSpace(key) && key != "(unknown)")
                        Register(spriteMap, key, modName);
                }
            }
        }

        private void Register(Dictionary<string, List<string>> map, string key, string modName)
        {
            if (!map.ContainsKey(key))
                map[key] = new List<string>();

            if (!map[key].Contains(modName))
                map[key].Add(modName);
        }

        private void ReportConflicts(ScanContext context)
        {
            foreach (var pair in techTypeMap)
            {
                if (pair.Value.Count > 1)
                {
                    context.AddNautilusWarning(
                        Severity.Error,
                        $"Duplicate TechType detected (reflection): \"{pair.Key}\" used by {string.Join(", ", pair.Value)}"
                    );
                }
            }

            foreach (var pair in craftTreeMap)
            {
                if (pair.Value.Count > 1)
                {
                    context.AddNautilusWarning(
                        Severity.Warning,
                        $"CraftTree conflict (reflection): \"{pair.Key}\" added by {string.Join(", ", pair.Value)}"
                    );
                }
            }

            foreach (var pair in spriteMap)
            {
                if (pair.Value.Count > 1)
                {
                    context.AddNautilusWarning(
                        Severity.Warning,
                        $"Sprite key conflict (reflection): \"{pair.Key}\" used by {string.Join(", ", pair.Value)}"
                    );
                }
            }
        }
    }
}
