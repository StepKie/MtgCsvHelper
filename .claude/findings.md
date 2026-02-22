# Code Review Findings

## High Priority

### ✅ 1. Static `IMtgApi.Default` — service locator anti-pattern
**File**: `MtgCsvHelper/Services/IMtgApi.cs`
~~`CardNameConverter` reached into a global static property instead of receiving the API through DI.~~

**Fixed**: `IMtgApi` is now threaded through `CardMapFactory.GenerateClassMap` → `PhysicalCardMap` →
`CardNameConverter`. The `static Default` property and `// FIXME` comment are deleted. Both `Program.cs`
files and `MtgApiFixture` no longer set it.

---

### ✅ 2. CI workflow uses wrong .NET version
**File**: `.github/workflows/dotnet.yml`
~~Build and test pipeline still targets `9.0.x` while the projects target `net10.0`.~~

**Fixed**: Updated `dotnet-version` to `10.0.x`. Also restricted GitHub Pages deploy to
`main` branch pushes only.

---

## Medium Priority

### 3. Dead code — `IMtgCardCsvHandlerService`
**Files**: `MtgCsvHelper/Services/IMtgCardCsvHandlerService.cs` and
`MtgCsvHelper/Services/MtgCardCsvHandlerService.cs`
Interface and implementation exist but are never registered in DI or used anywhere.

**Fix**: Delete both files.

---

### 4. Serilog not working in Blazor Program.cs
**File**: `MtgCsvHelper.BlazorWebAssembly/Program.cs`
- ✅ Dead debug variables (`csvConfigs`, `deckConfigsBuilder`) removed
- `Log.Information("Hello, Blazor, Serilog online!")` never appears — root cause unknown

**Likely cause**: Blazor WASM needs `Serilog.Sinks.BrowserConsole` (WriteTo.BrowserConsole) instead of
the standard console sink. The appsettings.json sink config may be targeting a sink that doesn't work
in-browser. Needs investigation.

---

### 5. Test coverage gaps
**File**: `MtgCsvHelper.Tests/`
- No tests for error cases: malformed CSV, invalid format names, missing required columns
- Deckbox round-trip test is commented out (`MtgCardCsvHandlerTests.cs:39`)
- ✅ Scryfall rate limiting fixed (shared `MtgApiFixture` via `ICollectionFixture`)
- ✅ `BaseTest` / `ApiBaseTest` split for clean test hierarchy
- No component tests for Blazor (tracked separately as task #1 — bUnit)

---

### 6. Improve error messages in `CardMapFactory`
**File**: `MtgCsvHelper/CardMapFactory.cs:20`
`GenerateClassMap` returns `null` for unsupported formats. Callers have to handle null
silently. Should throw a descriptive exception listing supported formats instead.

---

## Low Priority

### 7. TODO/tech debt scattered across codebase
- `Converters/CardNameConverter.cs:35` — Token encoding for Moxfield not implemented
- `Models/Collection.cs:28` — Rarity stats commented out, waiting for Scryfall enrichment
- `BlazorWebAssembly/Pages/MtgCsvProcessor.razor:16` — Multiple file upload not implemented
- `Console/Program.cs:46` — Error handling for CLI parse errors is a stub

### 8. `CardCondition` EnumClass pattern is verbose
**File**: `MtgCsvHelper/Models/CardCondition.cs`
Custom `EnumClass` base is unnecessarily complex. Could be a simple record hierarchy
or a string-keyed static class.

### 9. `MtgCardCsvHandler.ParseCollectionCsv` logs wrong value
**File**: `MtgCsvHelper/MtgCardCsvHandler.cs:27`
`Log.Information($"Parsing {csvFilePath} ...")` — when called with a `Stream`,
this logs the stream's type name, not something useful. Should log the format name instead.

---

## Other Completed Work (not tracked as findings above)

- ✅ Upgraded to .NET 10, updated all NuGet packages
- ✅ Replaced FluentAssertions with AwesomeAssertions
- ✅ Moved CLAUDE.md to `.claude/` folder
- ✅ `DeckConfig.SetNumber` made nullable (.NET 10 ConfigurationBinder breaking change)
- ✅ `WriteCollectionCsv` stream overload — Blazor no longer uses VFS roundtrip
- ✅ `_camelCase` private field naming enforced in editorconfig and applied to Razor component
