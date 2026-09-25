# Conflicts Scanner for Subnautica

Automated static conflict detector and ecosystem health scanner for **Subnautica** and **Subnautica: Below Zero** modlists.

Built with **.NET 8**, **Avalonia UI**, and **Mono.Cecil** for 100% static inspection—never executes untrusted mod assemblies or reflection constructors.

---

## Key Features & Conflict Detection

### 1. BepInEx Plugin & Dependency Graph Analysis
- **Duplicate Plugin GUIDs**: Identifies DLLs attempting to register duplicate `[BepInPlugin]` GUIDs (**Critical Impact**).
- **Circular Dependency Detection**: Builds a directed dependency graph from `[BepInDependency]` declarations and runs cycle detection algorithms to flag load order deadlocks (**Critical Impact**).
- **Missing Prerequisites**: Alerts on missing hard (required) and soft (optional) dependencies with target GUIDs.

### 2. Game Branch & Ecosystem Compatibility
- **Living Large (2.0+) vs. Legacy Branch**: Inspects game executable metadata and versions to detect whether the user is running modern Subnautica 2.0+ or the legacy branch.
- **QMod Incompatibility on Modern Subnautica**: Flags legacy QModManager mods attempting to run on Subnautica 2.0+, where QModManager is non-functional (**Critical Impact**).
- **Dual Mod Loader Collisions**: Warns if both BepInEx and QModManager are simultaneously installed in the same installation.
- **Framework Incompatibility**: Detects concurrent installation of legacy SMLHelper and modern Nautilus.

### 3. Deep Static Harmony Patch Inspection
- **IL Transpiler Collisions**: Pinpoints multiple mods applying IL transpilers to the same target method, which frequently corrupt instruction streams.
- **Prefix Priority Ties**: Identifies multiple prefixes attached to the same method with identical priority, resulting in non-deterministic execution order.
- **Prefix Suppression**: Detects when prefix patches return a boolean value (`bool`). In Harmony, returning `false` cancels original game methods and skips subsequent prefixes from other mods.

### 4. Nautilus & Custom Content Registration
- **TechType ID Collisions**: Inspects Nautilus `EnumHandler.AddEntry<TechType>` and `PrefabInfo.WithTechType` bytecode calls to detect duplicate custom item identifiers across mods.
- **CraftTree Path Collisions**: Detects multiple mods attaching recipes to the same crafting menu nodes.
- **Sprite Key Collisions**: Detects duplicate sprite registrations in the Nautilus texture atlas.

### 5. Filesystem & Asset Integrity
- **Namespace Isolation**: Internal mod files (`icon.png`, `config.json`) inside isolated mod folders are respected as private namespaces—eliminating false-positive path conflicts.
- **Bundled Assembly Collisions**: Detects when different mods bundle different copies of the same third-party library DLL.
- **Loose Plugin Shadowing**: Warns if a loose DLL in `BepInEx/plugins/` shadows an identically named mod directory.
- **Asset Integrity Checks**: Validates PNG magic byte headers, verifies JSON config formatting, and flags leftover development artifacts (`.bak`, `.tmp`).
- **Preloader Patchers**: Catalogs and inspects preloader patchers in `BepInEx/patchers/`.

---

## Finding Classification (Two-Axis Model)

Findings are evaluated on two orthogonal dimensions: **Impact** and **Confidence**.

| Impact | Definition | Examples |
| :--- | :--- | :--- |
| **Critical** | Fatal conflict; guaranteed crash or startup failure | Duplicate GUIDs, circular dependencies, QMods on Subnautica 2.0+ |
| **High** | Probable functional break or missing prerequisite | Concurrent transpilers, duplicate TechTypes, missing required dependency |
| **Medium** | Execution order ambiguity or potential suppression | Prefix priority ties, boolean prefix suppression, CraftTree path collision |
| **Low** / **Info**| Minor cosmetic issue or informational observation | Large files, leftover `.bak` files, optional dependencies |

| Confidence | Definition |
| :--- | :--- |
| **Observed** | Verified structural fact from static bytecode or filesystem metadata |
| **Probable** | High-certainty conflict based on static analysis heuristics |
| **Heuristic** | Pattern-based anomaly indicator |

---

## Interactive UI & Export

- **Real-Time Filter Bar**: Filter findings instantly by text search (mod name, resource key, explanation), category, or minimum impact level.
- **Dual-Format Export**:
  - **Formatted TXT**: Clean, human-readable text report grouped by category with recommended actions.
  - **Structured JSON**: Complete machine-readable array of findings for diagnostic tooling, bug reports, and community support.
- **Background Scanning with Cancellation**: Threaded scanning keeps the UI responsive with instant cancellation support.

---

## Supported Games & Launchers

- **Subnautica** (Steam & Epic Games Store)
- **Subnautica: Below Zero** (Steam & Epic Games Store)
- Custom installation paths supported via manual directory browse

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
