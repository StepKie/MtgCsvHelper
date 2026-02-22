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

### ✅ 10. `File.OpenWrite` doesn't truncate on overwrite
**File**: `MtgCsvHelper/MtgCardCsvHandler.cs:66`
~~`File.OpenWrite` opens for writing but leaves existing bytes beyond the new content, corrupting the output if a previous file was longer.~~

**Fixed**: Changed to `File.Create`, which always truncates.

---

### ✅ 11. `FileStream` resource leak in Console
**File**: `MtgCsvHelper.Console/Program.cs:39`
~~`new FileStream(fileName, FileMode.Open)` created inline — if the `StreamReader` constructor threw, the stream would not be disposed.~~

**Fixed**: Now uses the `ParseCollectionCsv(string)` overload directly, which manages its own stream lifetime.

---

### ✅ 12. Missing `EnsureSuccessStatusCode` in Scryfall token fetch
**File**: `MtgCsvHelper/Services/CachedMtgApi.cs`
~~HTTP errors from the token query were silently ignored — `JsonSerializer.Deserialize` would receive an error body and throw a misleading exception.~~

**Fixed**: Added `response.EnsureSuccessStatusCode()` before reading the body.

---

### ✅ 13. `DEFAULT_CLIENT` was a factory property (new `HttpClient` on every access)
**File**: `MtgCsvHelper/Services/CachedMtgApi.cs`
~~Declared as `=> new HttpClient()`, meaning a fresh socket-consuming instance was created on each call to `GetTokenCardNamesAsync`.~~

**Fixed**: Changed to `static readonly` field so the instance is shared and connection-pooled.

---

### ✅ 14. Dead `?? []` and null-forgiving `cards!`
**File**: `MtgCsvHelper/Services/CachedMtgApi.cs`
~~`.ToList() ?? []` is unreachable (`.ToList()` never returns null). `cards!.Data` suppressed a real nullability warning after `JsonSerializer.Deserialize`.~~

**Fixed**: Removed `?? []`. Replaced null-forgiving with `?? throw new InvalidOperationException(...)`.

---

### ✅ 15. Dead `Format` public property on `MtgCardCsvHandler`
**File**: `MtgCsvHelper/MtgCardCsvHandler.cs`
~~`public string Format { get; init; }` was never set or read — it always returned the default value.~~

**Fixed**: Deleted.

---

### 16. `LoadData()` `??=` assignments are not thread-safe
**File**: `MtgCsvHelper/Services/CachedMtgApi.cs`
`_sets ??= ...`, `_doubleFacedCardNames ??= ...` etc. are not atomic — two concurrent callers could both observe null, both issue API calls, and the second write wins. Low-impact since `LoadData` is only called once at startup, but still a latent race.

**Fix**: Use `Interlocked`-based pattern, a `SemaphoreSlim`, or move initialisation into the constructor / DI lifecycle.

---

### 17. `GetDefaultLoggerConfig` is a factory property
**File**: `MtgCsvHelper/AppLogging.cs`
`public static LoggerConfiguration GetDefaultLoggerConfig => new LoggerConfiguration()...` looks like a static getter but returns a new instance on every access. Properties that create new objects on every call are misleading.

**Fix**: Rename to `CreateDefaultLoggerConfig()` (method) to signal that it creates a new instance.

---

### 18. Non-seekable stream assumption in `ParseCollectionCsv`
**File**: `MtgCsvHelper/MtgCardCsvHandler.cs`
`stream.BaseStream.Position = 0` throws `NotSupportedException` if the caller passes a non-seekable stream (e.g. a network or compressed stream). Currently safe in practice, but fragile.

**Fix**: Check `stream.BaseStream.CanSeek` before resetting position, or document the seekable requirement.

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
