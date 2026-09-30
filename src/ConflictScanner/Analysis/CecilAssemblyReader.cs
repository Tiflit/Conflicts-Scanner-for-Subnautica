using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConflictScanner.Analysis
{
    public record BepInPluginInfo(
        string ModName,
        string AssemblyPath,
        string Guid,
        string Name,
        string Version,
        IReadOnlyList<BepInDependencyInfo> Dependencies
    );

    public record BepInDependencyInfo(
        string TargetGuid,
        bool IsHardDependency
    );

    public record HarmonyPatchTarget(
        string ModName,
        string TargetTypeName,
        string TargetMethodName,
        string PatchType, // Prefix, Postfix, Transpiler, Finalizer
        int Priority,
        bool ReturnsBoolean = false
    );

    public record NautilusRegistration(
        string ModName,
        string Type, // TechType, CraftTree, Sprite
        string Identifier
    );

    public class AssemblyAnalysisResult
    {
        public string ModName { get; init; } = string.Empty;
        public string AssemblyPath { get; init; } = string.Empty;
        public List<BepInPluginInfo> Plugins { get; } = new();
        public List<HarmonyPatchTarget> HarmonyPatches { get; } = new();
        public List<NautilusRegistration> NautilusRegistrations { get; } = new();
    }

    public static class CecilAssemblyReader
    {
        public static AssemblyAnalysisResult? AnalyzeAssembly(string filePath, string modName)
        {
            if (!File.Exists(filePath))
                return null;

            var result = new AssemblyAnalysisResult
            {
                ModName = modName,
                AssemblyPath = filePath
            };

            var resolver = new DefaultAssemblyResolver();
            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    resolver.AddSearchDirectory(dir);
                    string? parent = Directory.GetParent(dir)?.FullName;
                    if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                    {
                        resolver.AddSearchDirectory(parent);
                    }
                }
            }
            catch { }

            var readerParams = new ReaderParameters
            {
                ReadSymbols = false,
                ReadingMode = ReadingMode.Deferred,
                AssemblyResolver = resolver
            };

            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var assembly = AssemblyDefinition.ReadAssembly(stream, readerParams);

                foreach (var module in assembly.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        try
                        {
                            InspectType(type, modName, result);
                        }
                        catch
                        {
                            // Keep analyzing remaining types even if one is malformed
                        }
                    }
                }

                return result;
            }
            catch
            {
                // Return partial result if plugins/patches were already successfully extracted
                return (result.Plugins.Count > 0 || result.HarmonyPatches.Count > 0 || result.NautilusRegistrations.Count > 0)
                    ? result
                    : null;
            }
        }

        private static void InspectType(TypeDefinition type, string modName, AssemblyAnalysisResult result)
        {
            // 1. Inspect BepInPlugin attributes
            try
            {
                if (type.HasCustomAttributes)
                {
                    foreach (var attr in type.CustomAttributes)
                    {
                        if (attr.AttributeType?.FullName == "BepInEx.BepInPlugin" && attr.ConstructorArguments.Count >= 3)
                        {
                            string guid = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                            string name = attr.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                            string version = attr.ConstructorArguments[2].Value?.ToString() ?? string.Empty;

                            var deps = new List<BepInDependencyInfo>();
                            foreach (var depAttr in type.CustomAttributes)
                            {
                                if (depAttr.AttributeType?.FullName == "BepInEx.BepInDependency" && depAttr.ConstructorArguments.Count >= 1)
                                {
                                    string depGuid = depAttr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                                    bool isHard = true;
                                    if (depAttr.ConstructorArguments.Count >= 2 && depAttr.ConstructorArguments[1].Value is int flagVal)
                                    {
                                        isHard = flagVal == 1; // 1 = HardDependency, 2 = SoftDependency
                                    }
                                    if (!string.IsNullOrWhiteSpace(depGuid))
                                        deps.Add(new BepInDependencyInfo(depGuid.Trim(), isHard));
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(guid))
                            {
                                result.Plugins.Add(new BepInPluginInfo(modName, result.AssemblyPath, guid.Trim(), name.Trim(), version.Trim(), deps));
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Check for class-level HarmonyPatch
            string? classTargetType = null;
            string? classTargetMethod = null;
            try
            {
                if (type.HasCustomAttributes)
                {
                    (classTargetType, classTargetMethod) = ExtractHarmonyPatchTarget(type.CustomAttributes);
                }
            }
            catch { }

            // 3. Inspect methods
            try
            {
                if (type.HasMethods)
                {
                    foreach (var method in type.Methods)
                    {
                        try
                        {
                            InspectMethod(method, modName, classTargetType, classTargetMethod, result);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // Nested types
            try
            {
                if (type.HasNestedTypes)
                {
                    foreach (var nested in type.NestedTypes)
                    {
                        InspectType(nested, modName, result);
                    }
                }
            }
            catch { }
        }

        private static void InspectMethod(
            MethodDefinition method,
            string modName,
            string? classTargetType,
            string? classTargetMethod,
            AssemblyAnalysisResult result)
        {
            // Harmony patch detection on method
            string? patchType = null;
            try
            {
                if (method.HasCustomAttributes)
                {
                    foreach (var attr in method.CustomAttributes)
                    {
                        string? fn = attr.AttributeType?.FullName;
                        if (fn == "HarmonyLib.HarmonyPrefix" || fn == "Harmony.HarmonyPrefix") patchType = "Prefix";
                        else if (fn == "HarmonyLib.HarmonyPostfix" || fn == "Harmony.HarmonyPostfix") patchType = "Postfix";
                        else if (fn == "HarmonyLib.HarmonyTranspiler" || fn == "Harmony.HarmonyTranspiler") patchType = "Transpiler";
                        else if (fn == "HarmonyLib.HarmonyFinalizer" || fn == "Harmony.HarmonyFinalizer") patchType = "Finalizer";
                    }
                }

                if (patchType != null)
                {
                    var (methodTargetType, methodTargetMethod) = ExtractHarmonyPatchTarget(method.CustomAttributes);
                    string? targetType = methodTargetType ?? classTargetType;
                    string? targetMethod = methodTargetMethod ?? classTargetMethod;

                    if (!string.IsNullOrWhiteSpace(targetType) && !string.IsNullOrWhiteSpace(targetMethod))
                    {
                        int priority = ExtractHarmonyPriority(method.CustomAttributes)
                                       ?? (method.DeclaringType != null ? ExtractHarmonyPriority(method.DeclaringType.CustomAttributes) : null)
                                       ?? 400;

                        bool returnsBool = method.ReturnType?.FullName == "System.Boolean";
                        result.HarmonyPatches.Add(new HarmonyPatchTarget(modName, targetType, targetMethod, patchType, priority, returnsBool));
                    }
                }
            }
            catch { }

            // Method body instruction inspection (Nautilus calls)
            try
            {
                if (method.HasBody && method.Body != null && method.Body.Instructions.Count > 0)
                {
                    InspectInstructions(method.Body, modName, result);
                }
            }
            catch { }
        }

        private static void InspectInstructions(MethodBody body, string modName, AssemblyAnalysisResult result)
        {
            var instructions = body.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];
                if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
                {
                    if (instr.Operand is MethodReference methodRef && methodRef.DeclaringType != null)
                    {
                        string declType = methodRef.DeclaringType.FullName ?? string.Empty;
                        string mName = methodRef.Name ?? string.Empty;

                        // Nautilus / SMLHelper TechType registration
                        bool isTechType = (declType.Contains("EnumHandler") && mName == "AddEntry") ||
                                          (declType.Contains("PrefabInfo") && mName == "WithTechType") ||
                                          (declType.Contains("TechTypeHandler") && mName == "AddTechType");

                        if (isTechType)
                        {
                            string? strArg = FindPrecedingString(instructions, i);
                            if (!string.IsNullOrWhiteSpace(strArg) && strArg != "(unknown)")
                            {
                                result.NautilusRegistrations.Add(new NautilusRegistration(modName, "TechType", strArg));
                            }
                        }
                        else if (declType.Contains("CraftTreeHandler") && (mName == "AddCraftingNode" || mName == "AddNode"))
                        {
                            string? strArg = FindPrecedingString(instructions, i);
                            if (!string.IsNullOrWhiteSpace(strArg) && strArg != "(unknown)")
                            {
                                result.NautilusRegistrations.Add(new NautilusRegistration(modName, "CraftTree", strArg));
                            }
                        }
                        else if (declType.Contains("SpriteHandler") && (mName == "RegisterSprite" || mName == "Register"))
                        {
                            string? strArg = FindPrecedingString(instructions, i);
                            if (!string.IsNullOrWhiteSpace(strArg) && strArg != "(unknown)")
                            {
                                result.NautilusRegistrations.Add(new NautilusRegistration(modName, "Sprite", strArg));
                            }
                        }
                    }
                }
            }
        }

        private static string? FindPrecedingString(Mono.Collections.Generic.Collection<Instruction> instructions, int callIndex)
        {
            for (int i = callIndex - 1; i >= Math.Max(0, callIndex - 4); i--)
            {
                var instr = instructions[i];
                if (instr.OpCode == OpCodes.Ldstr && instr.Operand is string s)
                    return s;
            }
            return null;
        }

        private static (string? TargetType, string? TargetMethod) ExtractHarmonyPatchTarget(Mono.Collections.Generic.Collection<CustomAttribute> attributes)
        {
            string? targetType = null;
            string? targetMethod = null;

            foreach (var attr in attributes)
            {
                if (attr.AttributeType?.FullName == "HarmonyLib.HarmonyPatch" || attr.AttributeType?.FullName == "Harmony.HarmonyPatch")
                {
                    foreach (var arg in attr.ConstructorArguments)
                    {
                        if (arg.Value is TypeReference typeRef)
                        {
                            targetType = typeRef.Name;
                        }
                        else if (arg.Value is string str && !string.IsNullOrWhiteSpace(str))
                        {
                            targetMethod = str;
                        }
                    }
                }
            }

            return (targetType, targetMethod);
        }

        private static int? ExtractHarmonyPriority(Mono.Collections.Generic.Collection<CustomAttribute> attributes)
        {
            foreach (var attr in attributes)
            {
                if (attr.AttributeType?.FullName == "HarmonyLib.HarmonyPriority" || attr.AttributeType?.FullName == "Harmony.HarmonyPriority")
                {
                    if (attr.ConstructorArguments.Count > 0 && attr.ConstructorArguments[0].Value is int prio)
                        return prio;
                }
            }
            return null;
        }
    }
}
