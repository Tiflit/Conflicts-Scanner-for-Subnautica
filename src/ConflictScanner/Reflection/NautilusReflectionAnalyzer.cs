using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConflictScanner.Reflection
{
    public class NautilusReflectionAnalyzer
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
            Assembly asm = ReflectionUtils.LoadAssemblySafe(dllPath);
            if (asm == null)
            {
                context.AddNautilusWarning(
                    Severity.Info,
                    $"[{modName}] Failed to load assembly for reflection: {Path.GetFileName(dllPath)}"
                );
                return;
            }

            foreach (var type in asm.GetTypes())
            {
                foreach (var method in type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance))
                {
                    AnalyzeMethod(method, modName, context);
                }
            }
        }

        private void AnalyzeMethod(MethodInfo method, string modName, ScanContext context)
        {
            foreach (var (target, args) in ILReader.FindCalls(method))
            {
                if (NautilusSignatures.IsTechTypeRegistration(target))
                {
                    string id = args.FirstOrDefault() as string ?? "(unknown)";
                    Register(techTypeMap, id, modName);
                }

                if (NautilusSignatures.IsCraftTreeRegistration(target))
                {
                    string path = args.FirstOrDefault() as string ?? "(unknown)";
                    Register(craftTreeMap, path, modName);
                }

                if (NautilusSignatures.IsSpriteRegistration(target))
                {
                    string key = args.FirstOrDefault() as string ?? "(unknown)";
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
                        $"Duplicate TechType detected: \"{pair.Key}\" used by {string.Join(", ", pair.Value)}"
                    );
                }
            }

            foreach (var pair in craftTreeMap)
            {
                if (pair.Value.Count > 1)
                {
                    context.AddNautilusWarning(
                        Severity.Warning,
                        $"CraftTree conflict: \"{pair.Key}\" added by {string.Join(", ", pair.Value)}"
                    );
                }
            }

            foreach (var pair in spriteMap)
            {
                if (pair.Value.Count > 1)
                {
                    context.AddNautilusWarning(
                        Severity.Warning,
                        $"Sprite key conflict: \"{pair.Key}\" used by {string.Join(", ", pair.Value)}"
                    );
                }
            }
        }
    }
}
