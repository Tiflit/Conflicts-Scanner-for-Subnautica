# Conflicts Scanner for Subnautica

Automated utility to detect conflicts, incompatible assemblies, and asset anomalies in Subnautica and Subnautica: Below Zero modlists.

---

## Overview

Conflicts Scanner analyzes installed mods across modern (BepInEx 5 + Nautilus) and legacy mod frameworks to detect conflicts before launching the game.

### Supported Games & Launchers
- **Subnautica** (Steam & Epic Games Store)
- **Subnautica: Below Zero** (Steam & Epic Games Store)
- Custom installation paths supported via manual browse

---

## Key Features & Conflict Detection

- **BepInEx Plugin Metadata**:
  - Detects duplicate `[BepInPlugin]` GUIDs across installed assemblies (**Critical Impact**).
  - Verifies `[BepInDependency]` declarations and alerts on missing required dependencies.
- **Assembly & File Collisions**:
  - Detects bundled assembly collisions (different mods shipping duplicate DLL filenames that may shadow each other at runtime).
  - Verifies namespace isolation so internal mod assets (`icon.png`, `config.json`) do not trigger false-positive path conflicts.
- **Harmony Patch Analysis**:
  - Detects class-level and method-level `[HarmonyPatch]` declarations.
  - Warns on multiple transpilers targeting the same game method.
  - Identifies prefix priority conflicts on shared targets.
- **Framework & Ecosystem Verification**:
  - Identifies mutual incompatibility between modern Nautilus and legacy SMLHelper.
  - Inspects legacy QMod folders and missing `mod.json` manifests.
- **Asset Integrity & Anomalies**:
  - Verifies PNG magic headers to catch corrupted images.
  - Validates JSON config syntax and detects leftover/backup files (`.bak`, `.tmp`).

---

## Finding Classification (Impact & Confidence)

Findings are evaluated on two distinct axes:

1. **Impact**:
   - `Critical`: Guaranteed crash or load failure (e.g. duplicate plugin GUIDs, SMLHelper + Nautilus collision).
   - `High`: Probable game error or missing hard dependency.
   - `Medium`: Potential conflict or execution priority collision (e.g. prefix priority conflict).
   - `Low` / `Info`: Informational note or minor cosmetic anomaly.

2. **Confidence**:
   - `Observed`: Proven metadata fact (e.g. explicit duplicate GUIDs, missing file).
   - `Probable`: High-likelihood conflict based on structured static analysis.
   - `Heuristic`: Pattern-based observation.

---

## Building and Testing

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build Solution
```bash
dotnet restore ConflictScanner.sln
dotnet build ConflictScanner.sln --configuration Release
```

### Run Automated Tests
```bash
dotnet test ConflictScanner.sln
```

### Run Application
```bash
dotnet run --project src/ConflictScanner/ConflictScanner.csproj
```
