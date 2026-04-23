using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConflictScanner.Reflection
{
    /// <summary>
    /// Reflection-based Harmony patch analysis for Deep Scan mode.
    /// Detects prefix/postfix/transpiler/finalizer patches and reports conflicts.
    /// </summary>
    public class HarmonyReflectionAnalyzer
    {
        private class PatchInfo
        {
            public string ModName;
            public string PatchType;
            public int Priority;
            public MethodInfo TargetMethod;
        }

        private readonly List<PatchInfo> patches = new();

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

            DetectConflicts(context);
        }

        private void AnalyzeAssembly(string dllPath, string modName, ScanContext context)
        {
            Assembly asm = ReflectionUtils.LoadAssemblySafe(dllPath);
            if (asm == null)
            {
                context.AddHarmonyWarning(
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
            // Harmony patch attributes
            var prefix = method.GetCustomAttribute(Type.GetType("HarmonyLib.HarmonyPrefix"));
            var postfix = method.GetCustomAttribute(Type.GetType("HarmonyLib.HarmonyPostfix"));
            var transpiler = method.GetCustomAttribute(Type.GetType("HarmonyLib.HarmonyTranspiler"));
            var finalizer = method.GetCustomAttribute(Type.GetType("HarmonyLib.HarmonyFinalizer"));

            if (prefix == null && postfix == null && transpiler == null && finalizer == null)
                return;

            // Determine target method
            var harmonyPatch = method.GetCustomAttribute(Type.GetType("HarmonyLib.HarmonyPatch"));
            if (harmonyPatch == null)
                return;

            MethodInfo target = ResolveTargetMethod(harmonyPatch);
            if (target == null)
            {
                context.AddHarmonyWarning(
                    Severity.Info,
                    $"[{modName}] Could not resolve Harmony patch target for method {method.Name}"
                );
                return;
            }

            // Determine priority
            int priority = ExtractPriority(method);

            // Register patches
            if (prefix != null)
                patches.Add(new PatchInfo { ModName = modName, PatchType = "Prefix", Priority = priority, TargetMethod = target });

            if (postfix != null)
                patches.Add(new PatchInfo { ModName = modName, PatchType = "Postfix", Priority = priority, TargetMethod = target });

            if (transpiler != null)
                patches.Add(new PatchInfo { ModName = modName, PatchType = "Transpiler", Priority = priority, TargetMethod = target });

            if (finalizer != null)
                patches.Add(new PatchInfo { ModName = modName, PatchType = "Finalizer", Priority = priority, TargetMethod = target });
        }

        private MethodInfo ResolveTargetMethod(Attribute harmonyPatch)
        {
            // HarmonyPatch has fields like:
            // - originalType
            // - methodName
            // - argumentTypes
            var typeField = harmonyPatch.GetType().GetField("originalType");
            var nameField = harmonyPatch.GetType().GetField("methodName");
            var argsField = harmonyPatch.GetType().GetField("argumentTypes");

            Type targetType = typeField?.GetValue(harmonyPatch) as Type;
            string methodName = nameField?.GetValue(harmonyPatch) as string;
            Type[] args = argsField?.GetValue(harmonyPatch) as Type[];

            if (targetType == null || methodName == null)
                return null;

            if (args != null)
                return targetType.GetMethod(methodName, args);

            return targetType.GetMethod(methodName);
        }

        private int ExtractPriority(MethodInfo method)
        {
            var priorityAttr = method.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType().Name == "HarmonyPriority");

            if (priorityAttr == null)
                return 400; // Harmony default priority

            var field = priorityAttr.GetType().GetField("priority");
            if (field == null)
                return 400;

            return (int)field.GetValue(priorityAttr);
        }

        private void DetectConflicts(ScanContext context)
        {
            var groups = patches.GroupBy(p => p.TargetMethod);

            foreach (var group in groups)
            {
                var list = group.ToList();
                string targetName = $"{group.Key.DeclaringType.FullName}.{group.Key.Name}";

                // Multiple transpilers = high risk
                var transpilers = list.Where(p => p.PatchType == "Transpiler").ToList();
                if (transpilers.Count > 1)
                {
                    context.AddHarmonyWarning(
                        Severity.Error,
                        $"Multiple transpilers on {targetName}: {string.Join(", ", transpilers.Select(t => t.ModName))}"
                    );
                }

                // Prefix priority conflicts
                var prefixes = list.Where(p => p.PatchType == "Prefix").ToList();
                if (prefixes.Count > 1)
                {
                    var ordered = prefixes.OrderBy(p => p.Priority).ToList();
                    bool conflict = false;

                    for (int i = 0; i < ordered.Count - 1; i++)
                    {
                        if (ordered[i].Priority == ordered[i + 1].Priority)
                            conflict = true;
                    }

                    if (conflict)
                    {
                        context.AddHarmonyWarning(
                            Severity.Warning,
                            $"Prefix priority conflict on {targetName}: {string.Join(", ", prefixes.Select(p => $"{p.ModName} (prio {p.Priority})"))}"
                        );
                    }
                }

                // Mods skipping original execution
                if (prefixes.Any(p => p.Priority < 0))
                {
                    context.AddHarmonyWarning(
                        Severity.Info,
                        $"Mod skipping original execution on {targetName}: {string.Join(", ", prefixes.Where(p => p.Priority < 0).Select(p => p.ModName))}"
                    );
                }
            }
        }
    }
}
