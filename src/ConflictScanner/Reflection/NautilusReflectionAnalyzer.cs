using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConflictScanner.Reflection
{
    /// <summary>
    /// Reflection-based Nautilus analysis for Deep Scan mode.
    /// Detects TechType, CraftTree, and Sprite registrations.
    /// </summary>
    public class NautilusReflectionAnalyzer
    {
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
            if (NautilusSignatures.IsTechTypeRegistration(method))
            {
                context.AddNautilusWarning(
                    Severity.Info,
                    $"[{modName}] Registers a TechType via {method.DeclaringType.Name}.{method.Name}"
                );
            }

            if (NautilusSignatures.IsCraftTreeRegistration(method))
            {
                context.AddNautilusWarning(
                    Severity.Info,
                    $"[{modName}] Registers a CraftTree node via {method.DeclaringType.Name}.{method.Name}"
                );
            }

            if (NautilusSignatures.IsSpriteRegistration(method))
            {
                context.AddNautilusWarning(
                    Severity.Info,
                    $"[{modName}] Registers a sprite via {method.DeclaringType.Name}.{method.Name}"
                );
            }
        }
    }
}
