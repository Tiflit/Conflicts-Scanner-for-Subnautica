# Conflicts Scanner for Subnautica

**Version 0.2.0** — Automated static conflict detector, mod inventory tracker, and ecosystem health scanner for **Subnautica** and **Subnautica: Below Zero** modlists.

Built with **.NET 8**, **Avalonia UI 11**, and **Mono.Cecil** for 100% static inspection—never executes untrusted mod assemblies or reflection constructors.

---

## Key Features & Conflict Detection

### 1. Installed Mods Inventory & Health Dashboard
- **Comprehensive Mod Catalog**: Automatically discovers, catalogues, and displays all installed mods across your mod loaders (**BepInEx plugins**, legacy **QMods**, and **Preloader Patchers**).
- **Visual Health Badges**:
  - `CLEAN` (Green): Mod has no detected conflicts or structural anomalies.
  - `WARNING(S)` (Amber): Minor or cosmetic warnings detected (e.g. prefix priority ties, large assets).
  - `CONFLICT(S)` (Red): Critical or high-severity errors detected (e.g. duplicate GUIDs, missing dependencies, circular dependencies, branch incompatibilities).
- **Rich Metadata Display**: Shows the declared mod name, version string (e.g. `v1.0.0.54`), unique GUID or mod ID, loader type, and relative folder location.
- **One-Click Folder Navigation**: Direct **Open Folder** button opens any mod directory directly in your operating system's file manager.
- **Interactive Mod Filtering**: Filter mods instantly by text search (mod name, GUID, folder name, or assembly names) and status (*All*, *Clean only*, *With issues*).

### 2. BepInEx Plugin & Dependency Graph Analysis
- **Duplicate Plugin GUIDs**: Identifies DLLs attempting to register duplicate `[BepInPlugin]` GUIDs (**Critical Impact**).
- **Circular Dependency Detection**: Builds a directed dependency graph from `[BepInDependency]` declarations and runs cycle detection algorithms to flag load order deadlocks (**Critical Impact**).
- **Missing Prerequisites**: Alerts on missing hard (required) and soft (optional) dependencies with target GUIDs.
- **Core Library Fallback Resolution**: Guarantees core frameworks (`Nautilus`, `ConfigurationManager`, `SMLHelper`) are properly resolved by GUID even across custom packaging or stripped builds.

### 3. Game Branch & Ecosystem Compatibility
- **Living Large (2.0+) vs. Legacy Branch**: Inspects game executable metadata and Unity engine versions to determine whether the installation is modern Subnautica 2.0+ or the legacy branch.
- **QMod Incompatibility on Modern Subnautica**: Flags legacy QModManager mods attempting to run on Subnautica 2.0+, where QModManager is non-functional (**Critical Impact**).
- **Dual Mod Loader Collisions**: Warns if both BepInEx and QModManager are simultaneously installed in the same game directory.
- **Framework Incompatibility**: Detects concurrent installation of legacy SMLHelper and modern Nautilus.

### 4. Deep Static Harmony Patch Inspection
- **IL Transpiler Collisions**: Pinpoints multiple mods applying IL transpilers to the same target method, which frequently corrupt instruction streams.
- **Prefix Priority Ties**: Identifies multiple prefixes attached to the same method with identical priority, resulting in non-deterministic execution order.
- **Prefix Suppression**: Detects when prefix patches return a boolean value (`bool`). In Harmony, returning `false` cancels the original game method and suppresses subsequent prefixes from other mods.

### 5. Nautilus & Custom Content Registration
- **TechType ID Collisions**: Inspects Nautilus `EnumHandler.AddEntry<TechType>` and `PrefabInfo.WithTechType` bytecode calls to detect duplicate custom item identifiers across mods.
- **CraftTree Path Collisions**: Detects multiple mods attaching recipes to the same crafting menu nodes.
- **Sprite Key Collisions**: Detects duplicate sprite registrations in the Nautilus texture atlas.

### 6. Filesystem, Asset Integrity & Mod Manager Resilience
- **Mod Manager Noise Filtering**: Automatically filters out mod manager tracking markers such as `__folder_managed_by_vortex` and `.vortex*` files, preventing false duplicate hash findings.
- **Nested Folder Layout Support**: Recursively resolves multi-tier directory layouts used by Vortex and mod authors (e.g. `Tobey/SnapBuilder/SnapBuilder.dll`), mapping them cleanly to their parent mod.
- **Namespace Isolation**: Internal mod files (`icon.png`, `config.json`) inside isolated mod folders are respected as private namespaces—eliminating false-positive path conflicts.
- **Bundled Assembly Collisions**: Detects when different mods bundle different copies of the same third-party library DLL.
- **Loose Plugin Shadowing**: Warns if a loose DLL in `BepInEx/plugins/` shadows an identically named mod directory.
- **Asset Integrity Checks**: Validates PNG magic byte headers, verifies JSON config formatting, and flags leftover development artifacts (`.bak`, `.tmp`).
- **Preloader Patchers**: Catalogs and inspects preloader patchers in `BepInEx/patchers/`.

---

## Finding Classification (Two-Axis Model)

Findings are evaluated on two orthogonal dimensions: **Impact** and **Confidence**.

| Impact | Color Badge | Definition | Examples |
| :--- | :--- | :--- | :--- |
| **Critical** | Red (`#C62828`) | Fatal conflict; guaranteed crash or startup failure | Duplicate GUIDs, circular dependencies, QMods on Subnautica 2.0+ |
| **High** | Orange (`#EF6C00`) | Probable functional break or missing prerequisite | Concurrent transpilers, duplicate TechTypes, missing required dependency |
| **Medium** | Blue (`#1565C0`) | Execution order ambiguity or potential suppression | Prefix priority ties, boolean prefix suppression, CraftTree path collision |
| **Low** / **Info**| Gray (`#555555`) | Minor cosmetic issue or informational observation | Large files, leftover `.bak` files, optional dependencies |

| Confidence | Definition |
| :--- | :--- |
| **Observed** | Verified structural fact from static bytecode or filesystem metadata |
| **Probable** | High-certainty conflict based on static analysis heuristics |
| **Heuristic** | Pattern-based anomaly indicator |

---

## User Interface & Export

- **Tabbed Experience**:
  - **Findings**: Interactive, filtered list of all detected issues with severity badges, evidence, clear explanations, and recommended actions.
  - **Installed Mods**: Complete mod inventory table showing health status, loader, version, GUID, and one-click "Open Folder" buttons.
  - **Raw Report**: Human-readable diagnostic report with full mod counts, clean/issue statistics, categorized findings, suggestions, and patcher lists.
- **Real-Time Filter Bars**: Filter findings by category, minimum impact, or search text; filter mods by status and keyword.
- **Dual-Format Report Export**:
  - **Formatted TXT (`.txt`)**: Ready to paste into Discord or support forums.
  - **Structured JSON (`.json`)**: Machine-readable output for programmatic diagnostics and tooling.
- **Background Scanning with Cancellation**: Asynchronous scan pipeline with live cancellation support.

---

## Supported Games & Launchers

- **Subnautica** (Steam & Epic Games Store)
- **Subnautica: Below Zero** (Steam & Epic Games Store)
- Custom installation paths supported via manual directory browse or auto-detection

---

## Architecture & Analyzers

The scanner pipeline runs modular analyzers sequentially without loading untrusted code into the host process:

| Analyzer | Description |
| :--- | :--- |
| `BepInPluginAnalyzer` | Discovers BepInEx plugins, parses GUIDs/dependencies, checks for duplicate GUIDs, missing prerequisites, and dependency cycles. |
| `SMLHelperAnalyzer` | Detects SMLHelper vs Nautilus coexistence and framework deprecation. |
| `HarmonyAnalyzer` | Inspects Harmony annotations and methods for transpiler collisions, priority ties, and boolean suppression. |
| `NautilusAnalyzer` | Evaluates TechType, CraftTree, and Sprite atlas registrations via IL calls. |
| `QModAnalyzer` | Detects legacy QMods, parses `mod.json`, and checks Living Large 2.0+ compatibility and duplicate IDs. |
| `FileOverrideAnalyzer` | Scans filesystem assets, verifies magic headers and JSON syntax, detects bundled DLL collisions, and checks loose plugin shadowing. |
| `PatcherAnalyzer` | Catalogs preloader patchers in `BepInEx/patchers/`. |
| `SuggestionEngine` | Synthesizes findings into high-level actionable steps and updates mod health status metrics. |

---

## Building and Testing

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build Solution
```bash
dotnet restore ConflictScanner.sln
dotnet build ConflictScanner.sln --configuration Release
```

### Run Automated Tests (13 Unit Test Suites)
```bash
dotnet test ConflictScanner.sln
```

### Run Application
```bash
dotnet run --project src/ConflictScanner/ConflictScanner.csproj
```

### Publish Self-Contained Binary
```bash
dotnet publish src/ConflictScanner/ConflictScanner.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
