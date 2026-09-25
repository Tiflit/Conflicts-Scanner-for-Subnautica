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
    public class HarmonyReflectionAnalyzer : IAnalyzer
    {
        private class PatchInfo
        {
            public string ModName = string.Empty;
            public string PatchType = string.Empty;
            public int Priority;
            public MethodInfo TargetMethod = null!;
        }

        private readonly List<PatchInfo> patches = new();

        public void Run(ScanContext context)
        {
            if (context.Mode == ScanMode.Quick)
                return;

            patches.Clear();

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
            Assembly? asm = ReflectionUtils.LoadAssemblySafe(dllPath);
            if (asm == null)
            {
                context.AddHarmonyWarning(
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
                context.AddHarmonyWarning(
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
                        AnalyzeMethod(method, modName, context);
                    }
                    catch
                    {
                        // Individual method failure does not abort scan
                    }
                }
            }
        }

        private void AnalyzeMethod(MethodInfo method, string modName, ScanContext context)
        {
            Attribute[] methodAttrs;
            try
            {
                methodAttrs = method.GetCustomAttributes().ToArray();
            }
            catch
            {
                return;
            }

            bool hasPrefix = methodAttrs.Any(a => a.GetType().FullName == "HarmonyLib.HarmonyPrefix");
            bool hasPostfix = methodAttrs.Any(a => a.GetType().FullName == "HarmonyLib.HarmonyPostfix");
            bool hasTranspiler = methodAttrs.Any(a => a.GetType().FullName == "HarmonyLib.HarmonyTranspiler");
            bool hasFinalizer = methodAttrs.Any(a => a.GetType().FullName == "HarmonyLib.HarmonyFinalizer");

            if (!hasPrefix && !hasPostfix && !hasTranspiler && !hasFinalizer)
                return;

            Attribute[] classAttrs;
            try
            {
                classAttrs = method.DeclaringType?.GetCustomAttributes().ToArray() ?? Array.Empty<Attribute>();
            }
            catch
            {
                classAttrs = Array.Empty<Attribute>();
            }

            var harmonyPatch = methodAttrs.FirstOrDefault(a => a.GetType().FullName == "HarmonyLib.HarmonyPatch")
                               ?? classAttrs.FirstOrDefault(a => a.GetType().FullName == "HarmonyLib.HarmonyPatch");

            if (harmonyPatch == null)
                return;

            MethodInfo? target = ResolveTargetMethod(harmonyPatch);
            if (target == null)
            {
                context.AddHarmonyWarning(
                    Severity.Info,
                    $"[{modName}] Could not resolve Harmony patch target for method {method.Name}"
                );
                return;
            }

            var allAttrs = methodAttrs.Concat(classAttrs).ToArray();
            int priority = ExtractPriority(allAttrs);

            if (hasPrefix)
                patches.Add(new PatchInfo { ModName = modName, PatchType = "Prefix", Priority = priority, TargetMethod = target });

            if (hasPostfix)
                patches.Add(new PatchInfo { ModName = modName, PatchType = "Postfix", Priority = priority, TargetMethod = target });

            if (hasTranspiler)
                patches.Add(new PatchInfo { ModName = modName, PatchType = "Transpiler", Priority = priority, TargetMethod = target });

            if (hasFinalizer)
                patches.Add(new PatchInfo { ModName = modName, PatchType = "Finalizer", Priority = priority, TargetMethod = target });
        }

        private MethodInfo? ResolveTargetMethod(object harmonyPatch)
        {
            try
            {
                var type = harmonyPatch.GetType();

                object source = harmonyPatch;
                var infoMember = type.GetField("info", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) as MemberInfo
                                  ?? type.GetProperty("info", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (infoMember != null)
                {
                    var val = infoMember is FieldInfo fi ? fi.GetValue(harmonyPatch) : ((PropertyInfo)infoMember).GetValue(harmonyPatch);
                    if (val != null)
                        source = val;
                }

                Type sourceType = source.GetType();

                Type? targetType = GetMemberValue(source, sourceType, "declaringType") as Type
                                   ?? GetMemberValue(source, sourceType, "originalType") as Type;

                string? methodName = GetMemberValue(source, sourceType, "methodName") as string;
                Type[]? args = GetMemberValue(source, sourceType, "argumentTypes") as Type[];

                if (targetType == null || methodName == null)
                    return null;

                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

                if (args != null && args.Length > 0)
                    return targetType.GetMethod(methodName, bindingFlags, null, args, null);

                return targetType.GetMethod(methodName, bindingFlags);
            }
            catch
            {
                return null;
            }
        }

        private static object? GetMemberValue(object instance, Type type, string name)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return field.GetValue(instance);

            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.CanRead)
                return prop.GetValue(instance);

            return null;
        }

        private int ExtractPriority(object[] attrs)
        {
            var priorityAttr = attrs.FirstOrDefault(a => a.GetType().FullName == "HarmonyLib.HarmonyPriority");
            if (priorityAttr == null)
                return 400; // Harmony default

            var field = priorityAttr.GetType().GetField("priority", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                return 400;

            return (int)field.GetValue(priorityAttr)!;
        }

        private void DetectConflicts(ScanContext context)
        {
            var groups = patches.GroupBy(p => $"{p.TargetMethod.DeclaringType?.FullName ?? "Unknown"}.{p.TargetMethod.Name}");

            foreach (var group in groups)
            {
                var list = group.ToList();
                string targetName = group.Key;

                var transpilers = list.Where(p => p.PatchType == "Transpiler").ToList();
                if (transpilers.Count > 1)
                {
                    context.AddHarmonyWarning(
                        Severity.Error,
                        $"Multiple transpilers on {targetName}: {string.Join(", ", transpilers.Select(t => t.ModName))}"
                    );
                }

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
            }
        }
    }
}
