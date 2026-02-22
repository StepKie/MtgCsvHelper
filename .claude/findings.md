# Code Review Findings

## Open

### 5. Test coverage gaps
**File**: `MtgCsvHelper.Tests/`
- No tests for error cases: malformed CSV, invalid format names, missing required columns
- Deckbox round-trip test is commented out (`MtgCardCsvHandlerTests.cs:39`)
- No component tests for Blazor (tracked separately as task #1 — bUnit)

---

### 6. Improve error messages in `CardMapFactory`
**File**: `MtgCsvHelper/CardMapFactory.cs:20`
`GenerateClassMap` returns `null` for unsupported formats. Callers have to handle null
silently. Should throw a descriptive exception listing supported formats instead.

---

### 16. `LoadData()` `??=` assignments are not thread-safe
**File**: `MtgCsvHelper/Services/CachedMtgApi.cs`
`_sets ??= ...`, `_doubleFacedCardNames ??= ...` etc. are not atomic — two concurrent callers could both observe null, both issue API calls, and the second write wins. Low-impact since `LoadData` is only called once at startup, but still a latent race.

**Fix**: Use `Interlocked`-based pattern, a `SemaphoreSlim`, or move initialisation into the constructor / DI lifecycle.

---

### 18. Non-seekable stream assumption in `ParseCollectionCsv`
**File**: `MtgCsvHelper/MtgCardCsvHandler.cs`
`stream.BaseStream.Position = 0` throws `NotSupportedException` if the caller passes a non-seekable stream (e.g. a network or compressed stream). Currently safe in practice, but fragile.

**Fix**: Check `stream.BaseStream.CanSeek` before resetting position, or document the seekable requirement.

---

### 7. TODO/tech debt scattered across codebase
- `Converters/CardNameConverter.cs:35` — Token encoding for Moxfield not implemented
- `Models/Collection.cs:28` — Rarity stats commented out, waiting for Scryfall enrichment
- `BlazorWebAssembly/Pages/MtgCsvProcessor.razor:16` — Multiple file upload not implemented
- `Console/Program.cs:46` — Error handling for CLI parse errors is a stub

---

### 8. `CardCondition` EnumClass pattern is verbose
**File**: `MtgCsvHelper/Models/CardCondition.cs`
Custom `EnumClass` base is unnecessarily complex. Could be a simple record hierarchy
or a string-keyed static class.

---

### 9. `MtgCardCsvHandler.ParseCollectionCsv` logs wrong value
**File**: `MtgCsvHelper/MtgCardCsvHandler.cs:27`
`Log.Information($"Parsing {csvFilePath} ...")` — when called with a `Stream`,
this logs the stream's type name, not something useful. Should log the format name instead.

---

## Resolved

- ✅ **#1** `IMtgApi.Default` static service locator removed — `IMtgApi` threaded through `CardMapFactory` → `PhysicalCardMap` → `CardNameConverter`
- ✅ **#2** CI workflow `.NET` version fixed (`9.0.x` → `10.0.x`), Pages deploy restricted to `main`
- ✅ **#3** Deleted dead `IMtgCardCsvHandlerService` / `MtgCardCsvHandlerService`
- ✅ **#4** Serilog in Blazor fixed — `BrowserConsole` sink, dev-only via `appsettings.Development.json`
- ✅ **#5 (partial)** Scryfall rate limiting fixed (shared `MtgApiFixture`), `BaseTest`/`ApiBaseTest` split, `StreamRoundTripTest` added
- ✅ **#10** `File.OpenWrite` → `File.Create` (truncates existing files)
- ✅ **#11** `FileStream` resource leak in Console — uses string overload now
- ✅ **#12** Missing `EnsureSuccessStatusCode` in Scryfall token fetch
- ✅ **#13** `DEFAULT_CLIENT` property → `static readonly` field (shared, connection-pooled)
- ✅ **#14** Dead `?? []` removed; `cards!` null-forgiving replaced with `?? throw`
- ✅ **#15** Dead `Format` property on `MtgCardCsvHandler` deleted
- ✅ **#17** `GetDefaultLoggerConfig` property renamed to `CreateDefaultLoggerConfig()` method
- ✅ Upgraded to .NET 10, updated all NuGet packages
- ✅ Replaced FluentAssertions with AwesomeAssertions
- ✅ `DeckConfig.SetNumber` made nullable (.NET 10 ConfigurationBinder breaking change)
- ✅ `WriteCollectionCsv` stream overload — Blazor no longer uses VFS roundtrip
- ✅ `_camelCase` private field naming enforced in editorconfig and Razor component
