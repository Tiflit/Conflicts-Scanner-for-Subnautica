using System;
using System.Linq;

namespace ConflictScanner
{
    public static class SuggestionEngine
    {
        public static void Generate(ScanContext context)
        {
            var findings = context.Findings;
            if (findings.Count == 0)
            {
                context.Suggestions.Add("No conflicts or compatibility issues detected. Your modlist appears healthy!");
                return;
            }

            int criticalCount = findings.Count(f => f.Impact == Impact.Critical);
            int highCount = findings.Count(f => f.Impact == Impact.High);

            if (criticalCount > 0)
            {
                context.Suggestions.Add(
                    $"CRITICAL: Detected {criticalCount} fatal conflict(s). Game launch is likely to fail or crash until these are resolved."
                );
            }

            bool hasDuplicateGuids = findings.Any(f => f.Category == "Metadata" && f.Impact == Impact.Critical);
            if (hasDuplicateGuids)
            {
                context.Suggestions.Add(
                    "DUPLICATE GUIDS: Multiple DLLs share the same BepInPlugin GUID. Remove redundant copies or old versions."
                );
            }

            bool hasCircularDeps = findings.Any(f => f.Category == "Dependencies" && f.Impact == Impact.Critical);
            if (hasCircularDeps)
            {
                context.Suggestions.Add(
                    "CIRCULAR DEPENDENCY: A cyclic dependency loop exists between plugins. BepInEx cannot determine load order and will fail to load these mods."
                );
            }

            bool hasMissingDeps = findings.Any(f => f.Category == "Metadata" && f.Explanation.Contains("Missing"));
            if (hasMissingDeps)
            {
                context.Suggestions.Add(
                    "MISSING DEPENDENCIES: One or more mods declare missing dependencies. Check the findings list and install the required prerequisites."
                );
            }

            bool hasBranchIncompatibility = findings.Any(f => f.Category == "Compatibility" && f.ResourceKey == "QMods_On_Modern_Branch");
            if (hasBranchIncompatibility)
            {
                context.Suggestions.Add(
                    "BRANCH INCOMPATIBILITY: QMods cannot run on Subnautica 2.0+ (Living Large). Remove legacy QMods and install BepInEx/Nautilus equivalents, or revert to the Steam legacy branch."
                );
            }

            bool hasDualLoaders = findings.Any(f => f.Category == "Compatibility" && f.ResourceKey == "Dual_Mod_Loaders_Detected");
            if (hasDualLoaders)
            {
                context.Suggestions.Add(
                    "DUAL LOADERS: Both BepInEx and QModManager are installed. Having two active mod loaders causes crashes and duplicate patches; remove the unused loader."
                );
            }

            bool hasHarmonyTranspilerConflict = findings.Any(f => f.Category == "Harmony" && f.Impact == Impact.High);
            if (hasHarmonyTranspilerConflict)
            {
                context.Suggestions.Add(
                    "HARMONY COLLISION: Multiple transpilers target the same game method. These mods modify the exact same instructions and may not work together without a compatibility patch."
                );
            }

            bool hasTechTypeConflict = findings.Any(f => f.Category == "Nautilus" && f.Impact == Impact.High);
            if (hasTechTypeConflict)
            {
                context.Suggestions.Add(
                    "TECHTYPE COLLISION: Multiple mods register identical TechType identifiers. Recipes or custom items may overwrite each other."
                );
            }

            bool hasSmlAndNautilus = findings.Any(f => f.Category == "SMLHelper" && f.Impact >= Impact.High);
            if (hasSmlAndNautilus)
            {
                context.Suggestions.Add(
                    "FRAMEWORK INCOMPATIBILITY: SMLHelper and Nautilus are both installed. SMLHelper is deprecated for Subnautica 2.0+; replace legacy SMLHelper mods with Nautilus versions."
                );
            }

            if (context.QModWarnings.Count > 0 && !hasBranchIncompatibility && !hasDualLoaders)
            {
                context.Suggestions.Add(
                    "LEGACY QMODS: QModManager mods detected. Modern Subnautica uses BepInEx. Legacy QMods will not load unless you are running the legacy game branch."
                );
            }
        }
    }
}
