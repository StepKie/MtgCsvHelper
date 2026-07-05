# Changelog

All notable changes to this project are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Unreleased work targets the next minor version once a coherent feature set is ready; bugfixes ship on top as patch releases.

## [Unreleased]

### Fixed

- **Non-English rows are no longer dropped over their localized card name** ([#103](https://github.com/StepKie/MtgCsvHelper/issues/103)). When a row's `(Set, Collector#)` resolves to a printing but the localized name (e.g. Italian `Fulmine`) doesn't match the English-only catalog, the row is now kept with the catalog's English name and a Warning, instead of being skipped as a name-mismatch error. Applies only to rows whose Language column explicitly says non-English; English rows keep the strict corruption guard.

### Changed

- **Exports match each site's native column layout** ([#134](https://github.com/StepKie/MtgCsvHelper/issues/134)). Every writable format now emits the site's full header set in the site's exact column order — instead of only the modeled columns in our own order. Strict, order-sensitive importers (Archidekt, TCGplayer) accept the files, and they diff cleanly against real site exports. Catalog-derived columns are filled (rarity, Multiverse Id, TCGplayer product id — extending the Scryfall id from 1.5.0); the rest are left blank. The native layout is declared per format in `appsettings.json` and anchored to a captured export by test.

### Internal

- **bUnit component tests for the web UI** ([#70](https://github.com/StepKie/MtgCsvHelper/issues/70)). New `MtgCsvHelper.BlazorWebAssembly.Tests` project covering the converter page: format dropdown contents, auto-detect on file upload, input/output collision snapping, the header-mismatch error alert, and the catalog-load failure/retry path.

## [1.5.0] — 2026-07-03

### Added

- **TCGplayer variant names on export** ([#23](https://github.com/StepKie/MtgCsvHelper/issues/23)). Borderless / Showcase / Extended Art printings now get TCGplayer's parenthetical suffix appended to a `Name` column (`Orcish Bowmasters (Borderless)`), so TCGplayer's name-matching importer resolves the correct printing instead of the default one; `Simple Name` stays plain. Derived from the bundled catalog's `border_color` / `frame_effects` — no extra lookups. (Pending verification against a real TCGplayer import; foil treatments and Retro Frame are follow-ups.)

### Fixed

- **Foreign-language and retired-set-code imports are no longer dropped** ([#92](https://github.com/StepKie/MtgCsvHelper/issues/92), [#103](https://github.com/StepKie/MtgCsvHelper/issues/103)). Rows whose `(Set, Collector#)` coordinate isn't in the Scryfall catalog — foreign-language reprints (Italian Legends and The Dark, Foreign White/Black Border) and retired set codes — are now rewritten by card name to an importable printing and kept, with a Warning noting the rewrite, instead of being skipped as errors. The rewritten printing is the catalog's default (usually the English one), so the foreign-language identity is not preserved.
- **Cards resolve by Scryfall ID when the source CSV carries one** ([#127](https://github.com/StepKie/MtgCsvHelper/issues/127), [#130](https://github.com/StepKie/MtgCsvHelper/issues/130)). Manabox and Topdecked exports include a Scryfall ID column; it is now trusted ahead of `(Set, Collector#)` — which some sites reshape on export — pinning the exact printing and its canonical name, set, and rarity.
- **Double-faced and reversible card names no longer mismatch** ([#94](https://github.com/StepKie/MtgCsvHelper/issues/94)). A front-face-only import (`Mountain`) or a same-name reversible printing now matches the full Scryfall name (`Mountain // Mountain`) and is canonicalized, instead of being dropped as a name mismatch.
- **DragonShield guild-kit set names, DragonShield Simplified Chinese, and Deckbox etched finish** ([#104](https://github.com/StepKie/MtgCsvHelper/issues/104), [#126](https://github.com/StepKie/MtgCsvHelper/issues/126)). Guild-kit set names now round-trip, the Simplified Chinese language code maps correctly, and Deckbox etched printings are recognized.

### Changed

- **Tri-state card finish (normal / foil / etched)** ([#68](https://github.com/StepKie/MtgCsvHelper/issues/68)). Replaces the previous boolean foil flag, so etched printings round-trip through formats that support an etched tier and degrade to foil in those that don't.
- **Stricter condition vocabulary** ([#69](https://github.com/StepKie/MtgCsvHelper/issues/69)). An unmapped condition string is now rejected with an explicit error instead of being silently accepted.
- **Exporter-decorated names are accepted** (PR [#113](https://github.com/StepKie/MtgCsvHelper/pull/113)). A name that extends the canonical printing name (`Beast Token (4/4)`, `Morph Creature`) matches the base printing rather than being dropped as a mismatch.
- **Per-rarity collection stats** (PR [#112](https://github.com/StepKie/MtgCsvHelper/pull/112)). Rarity is backfilled from the catalog, feeding a typed rarity breakdown in the import summary.

### Internal

- **Master reference collection and per-format ground-truth CSVs** ([#117](https://github.com/StepKie/MtgCsvHelper/issues/117), [#119](https://github.com/StepKie/MtgCsvHelper/issues/119)). A single `master.csv` drives committed per-format reference exports; every read+write format round-trips it, and a per-fixture bijection assertion guards against silent row loss. Grown to cover etched finishes, an 11-language sweep, borderless and non-transform DFCs, and weird special-products.

## [1.4.1] — 2026-05-31

### Fixed

- **Dragon Shield Ravnica Guild Kit set codes** ([#102](https://github.com/StepKie/MtgCsvHelper/issues/102), PR [#105](https://github.com/StepKie/MtgCsvHelper/pull/105)). Dragon Shield emits per-guild codes (`GK1_DIMIR`, `GK2_AZORIU`, …) for the Ravnica guild kits; the Scryfall set is a single `gk1`/`gk2` sharing collector numbers across guilds. These codes no longer fail catalog validation — `DragonShieldCodeReadConverter` collapses `GK[12]_*` to the canonical code on read (the guild suffix is cosmetic), so the cards import instead of being dropped.
- **Dragon Shield variant foil treatments** ([#102](https://github.com/StepKie/MtgCsvHelper/issues/102), PR [#105](https://github.com/StepKie/MtgCsvHelper/pull/105)). `FinishConverter` now treats any value containing "foil" (`Surge Foil`, `Step and Compleat Foil`, …) as foil, instead of matching a fixed allowlist that went stale with each new WotC treatment.

### Changed

- **"Report to GitHub" embeds the error data inline** (PR [#107](https://github.com/StepKie/MtgCsvHelper/pull/107)). The prefilled issue now carries a normalized reason histogram (per-row `#N` collapsed so the same set/value aggregates) plus the failing rows, so there's nothing to attach in the common case; large reports fall back to the downloadable CSV. Extracted into a unit-tested `ImportErrorReport`.

### Internal

- **Site-behavior documentation** (PR [#105](https://github.com/StepKie/MtgCsvHelper/pull/105)). Recorded the observed Dragon Shield behaviors — Guild Kit codes, variant foils, and the export-side CSV importer matching by name rather than honoring canonical set codes. Follow-ups filed: [#103](https://github.com/StepKie/MtgCsvHelper/issues/103) (foreign-language imports), [#104](https://github.com/StepKie/MtgCsvHelper/issues/104) (Dragon Shield export native codes).

## [1.4.0] — 2026-05-21

### Added

- **MTGO CSV format support** ([#76](https://github.com/StepKie/MtgCsvHelper/pull/76)). 2-letter legacy set codes (MI/VI/TE/EX) canonicalized to 3-letter Scryfall codes via Scryfall's `mtgo_code` field; `N/M` collector numbers stripped.
- **Deckbox set-name + edition-code aliasing** ([#31](https://github.com/StepKie/MtgCsvHelper/issues/31), PR [#86](https://github.com/StepKie/MtgCsvHelper/pull/86)). Writes emit Deckbox-curated edition names ("Magic 2014 Core Set", "Innistrad: Crimson Vow Commander", "Promo Pack: Kamigawa: Neon Dynasty", "Extras: Modern Horizons 2"). Reads translate Deckbox-internal codes (EX_127, PP_NEO) and legacy aliases (1E↔LEA, AL↔ALL, PLIST↔PLST) back to Scryfall. Two scraped resources (`deckbox-set-aliases.json`, `deckbox-code-aliases.json`) generated by `tools/MtgCsvHelper.RefreshReferenceData -- deckbox-aliases`.
- **Import-issue row detail panel** ([#91](https://github.com/StepKie/MtgCsvHelper/issues/91), PR [#95](https://github.com/StepKie/MtgCsvHelper/pull/95)). Each error/warning group in the import-issues table now shows the raw CSV line beneath the issue summary, so users can identify exactly which row of their source file produced the problem.
- **Dragon Shield 12-column format support** ([#97](https://github.com/StepKie/MtgCsvHelper/issues/97), PR [#98](https://github.com/StepKie/MtgCsvHelper/pull/98)). DS changed its required header to `Folder Name, Quantity, Trade Quantity, Card Name, Set Code, Set Name, Card Number, Condition, Printing, Language, Price Bought, Date Bought` and now rejects rows with null cells in declared columns. New `RequiresWriteDefaults` config flag drives a pre-write pass that stamps `PriceBought=0`, `DateBought=today`, `Folder="Imported"` on null slots. `DateBoughtConfiguration.Formats` accepts multiple parse formats so DS's own `M/d/yyyy` historical exports still read.

### Changed

- **Parsed cards validated against the Scryfall catalog at import time** ([#75](https://github.com/StepKie/MtgCsvHelper/pull/75)). Rows whose `(Set, CollectorNumber)` doesn't resolve, whose name doesn't match the printing, or whose foil flag asks for a non-existent finish are dropped with an explicit Error issue.
- **Post-parse enricher pipeline extracted into discrete steps** ([#79](https://github.com/StepKie/MtgCsvHelper/pull/79)). `SetInfoEnricher` / `CatalogValidator` / `CardmarketIdEnricher` replace the previous inline post-parse loop. `SetInfoEnricher` now overwrites `SetName` from the catalog whenever the set code resolves (instead of only backfilling missing names), so a Deckbox import's "Extras: X" doesn't survive the round-trip into other formats.
- **Dropdown labels and read/write filtering** ([#74](https://github.com/StepKie/MtgCsvHelper/pull/74)). MTGGoldfish/Archidekt display correctly; input/output selectors filter by capability so write-only Card Kingdom and read-only Cardmarket only appear where applicable.
- **Per-row issue deduplication** ([PR #96](https://github.com/StepKie/MtgCsvHelper/pull/96)). When a row produces both errors and warnings, only the errors are surfaced — warnings on already-dropped rows would be noise.

### Fixed

- **Blazor app couldn't load `appsettings.json`** ([#82](https://github.com/StepKie/MtgCsvHelper/issues/82), PR [#83](https://github.com/StepKie/MtgCsvHelper/pull/83)). The build-time staging target ran in execution phase, after the static-web-assets pipeline had already enumerated `wwwroot/`. Fresh CI checkouts therefore shipped without the file in the service-worker manifest, and every conversion in the deployed app failed with `Format 'X' configuration not found`. The file is now committed; the build target keeps it in sync with `MtgCsvHelper/appsettings.json`; `AppsettingsParityTests` guards against drift.
- **Split cards stripped on short-name export** ([#97](https://github.com/StepKie/MtgCsvHelper/issues/97), PR [#98](https://github.com/StepKie/MtgCsvHelper/pull/98)). `Commit // Memory`, `Fire // Ice`, `Wear // Tear`, etc. were truncated to their first half on Dragon Shield / TCGPlayer / MTGGoldfish / CardKingdom exports. New `IReferenceCardCatalog.GetLayoutByName` lookup lets `CardNameConverter` distinguish split layouts (keep both halves — both ARE the front face) from DFC-style layouts (transform/modal_dfc/meld/flip — strip to front face).
- **`LanguageConverter` silently shifted CSV columns** ([#97](https://github.com/StepKie/MtgCsvHelper/issues/97), PR [#98](https://github.com/StepKie/MtgCsvHelper/pull/98)). Returning `null` from `ITypeConverter.ConvertToString` makes CsvHelper skip the field entirely, shifting every subsequent column one slot left and corrupting the row. Now returns `""` on no-match; comment guards against future "fix it back to null" attempts.

### Internal

- **README rewrite + CHANGELOG.md adopted + `/whats-new` page** ([#85](https://github.com/StepKie/MtgCsvHelper/issues/85), PR [#89](https://github.com/StepKie/MtgCsvHelper/pull/89)). README focuses on what the tool does and how to use it; CHANGELOG follows Keep-a-Changelog; the Blazor app's new `/whats-new` page surfaces recent changes to end users.
- CI: build + test on `develop` branch, cache the catalog bundle, allow the auto-review bot to post bundled reviews, gate summary comments to initial review ([#77](https://github.com/StepKie/MtgCsvHelper/pull/77), [#78](https://github.com/StepKie/MtgCsvHelper/pull/78), [#80](https://github.com/StepKie/MtgCsvHelper/pull/80), [#81](https://github.com/StepKie/MtgCsvHelper/pull/81)).

## [1.3.0] — 2026-05-16

### Added

- **Archidekt CSV import/export support** ([#41](https://github.com/StepKie/MtgCsvHelper/issues/41), PR [#66](https://github.com/StepKie/MtgCsvHelper/pull/66)). Full read/write with 11-language map, TCGPlayer-standard 5-condition vocabulary (`NM/LP/MP/HP/D`), 3-value Finish enum including Etched.
- **Per-format test fixture framework** ([#61](https://github.com/StepKie/MtgCsvHelper/issues/61)). Reference-collection fixtures for all 9 readable formats. Auto-discovered theories pin invariants per fixture suffix: `*-reference-collection.csv` (parses cleanly), `*-rejected.csv` (all rows error), `*-mixed-warnings-and-errors.csv` (no silent swallow), `*-field-fidelity.csv` (field-level equality against hand-built reference cards).
- **`CONVERSION_LIMITATIONS.md`** — user-facing cross-format data-loss summary. Format support matrix plus per-axis breakdowns (condition collapse, language drops, DragonShield variant foils, Deckbox edition aliases, DFC name shapes, Cardmarket's `idProduct`-only model). Linked from `README.md`.

### Changed

- **Parser tightening** — `FinishConverter`, `LanguageConverter`, `CardConditionConverter` now reject unmapped values explicitly instead of silently emitting nulls. Surfaced as Error-severity issues on import. Case-insensitive comparison via a new `CsvMatch.MatchesConfig` extension.
- **Condition-collision fix** — formats whose `Mint` or `Excellent` collapses to the same string as `NearMint` (Archidekt, Moxfield, TopDecked, Deckbox) declare those entries as `null` in `appsettings.json`. Write path falls back to `NearMint`; read path doesn't match a null entry. Eliminates the switch-arm-order brittleness that caused Archidekt `"NM"` cards to round-trip as `MINT`.
- **Single source of truth for `appsettings.json`** — the hand-maintained duplicate at `MtgCsvHelper.BlazorWebAssembly/wwwroot/appsettings.json` is gone. Blazor stages a copy from the core library via a build target. *(This staging mechanism turned out to have a deployed-build bug — see the [#82](https://github.com/StepKie/MtgCsvHelper/issues/82) fix in `[Unreleased]`.)*

### Internal

- `Resources/Scryfall/` deleted (obsoleted by the bundled catalog from PR series [#48](https://github.com/StepKie/MtgCsvHelper/pull/48)).
- `Resources/SampleCsvs/Samples/` consolidated into `Tests/`; `*-sample.csv` renamed to `*-field-fidelity.csv` to make test purpose self-documenting.

## [1.2.0] — 2026-05-14

### Added

- **MudBlazor UI overhaul + format auto-detect** ([#45](https://github.com/StepKie/MtgCsvHelper/issues/45), PR [#62](https://github.com/StepKie/MtgCsvHelper/pull/62)). Modern dark+light theme, sortable Moxfield-style results table, card-art hover preview, Scryfall click-through. Input format auto-detected from CSV headers in both Web and Console.
- **In-page error UI when the catalog bundle fails to load** ([#56](https://github.com/StepKie/MtgCsvHelper/issues/56), PR [#58](https://github.com/StepKie/MtgCsvHelper/pull/58)). Replaces silent boot-spinner-forever with a clear error card + Reload + Report buttons.
- **Background-load catalog + WASM AOT** ([#59](https://github.com/StepKie/MtgCsvHelper/issues/59), PR [#63](https://github.com/StepKie/MtgCsvHelper/pull/63)). Shell renders within ~2 s instead of ~30 s. Catalog loads in the background with progress bar; Convert enables when ready. AOT compilation enabled — bigger payload, much faster runtime parse.
- New app icon (two CSV documents with U/G mana symbols + conversion arrow), simpler favicon.
- Feedback link in the appbar → GitHub Discussions; Report-to-GitHub button on conversions with errors (downloads error CSV + opens pre-filled issue).
- About page rewritten as a concise user manual.

### Compatibility

- AOT build means the first visit downloads ~5 MB more (Brotli framework: 4.7 → 9.9 MB). Cached after.

## [1.1.0] — 2026-05-10

### Added

- **Cardmarket as read-only import format** with Scryfall enrichment by `idProduct`.
- **Fault-tolerant import** — per-row try/catch, warnings, structured `ParseResult` (cards + issues).

### Changed

- Console UX: cwd-independent config resolution, path-friendly `-f` flag, honest error reporting.
- Cardmarket: unresolved rows are dropped instead of leaving stubs in the output.
- Classmaps refactored to remove the `CARDKINGDOM` boolean flag.

## [1.0.7] — 2026-02-22

### Added

- Improve Blazor load time and add service-worker update banner ([#35](https://github.com/StepKie/MtgCsvHelper/pull/35)).

## [1.0.4] — 2024-10-09

### Added

- Upgrade projects to .NET 9 ([#16](https://github.com/StepKie/MtgCsvHelper/pull/16); later reverted in [#18](https://github.com/StepKie/MtgCsvHelper/pull/18)).
- Dependency injection: default request headers for the Scryfall client ([#21](https://github.com/StepKie/MtgCsvHelper/pull/21)).

## [1.0.2] — 2024-09-08

Maintenance release.

## [1.0.0] — 2024-01-17

First stable release. Web app + Console app.

## Earlier releases

- [0.2.0] — 2023-12-13
- [0.1.0] — 2022-12-02
- [0.0.3] — 2022-11-15

[Unreleased]: https://github.com/StepKie/MtgCsvHelper/compare/1.5.0...HEAD
[1.5.0]: https://github.com/StepKie/MtgCsvHelper/compare/1.4.1...1.5.0
[1.4.1]: https://github.com/StepKie/MtgCsvHelper/compare/1.4.0...1.4.1
[1.4.0]: https://github.com/StepKie/MtgCsvHelper/compare/1.3.0...1.4.0
[1.3.0]: https://github.com/StepKie/MtgCsvHelper/compare/1.2.0...1.3.0
[1.2.0]: https://github.com/StepKie/MtgCsvHelper/compare/1.1.0...1.2.0
[1.1.0]: https://github.com/StepKie/MtgCsvHelper/compare/1.0.7...1.1.0
[1.0.7]: https://github.com/StepKie/MtgCsvHelper/compare/1.0.4...1.0.7
[1.0.4]: https://github.com/StepKie/MtgCsvHelper/compare/1.0.2...1.0.4
[1.0.2]: https://github.com/StepKie/MtgCsvHelper/compare/1.0.0...1.0.2
[1.0.0]: https://github.com/StepKie/MtgCsvHelper/compare/0.2.0...1.0.0
[0.2.0]: https://github.com/StepKie/MtgCsvHelper/compare/0.1.0...0.2.0
[0.1.0]: https://github.com/StepKie/MtgCsvHelper/compare/0.0.3...0.1.0
[0.0.3]: https://github.com/StepKie/MtgCsvHelper/releases/tag/0.0.3
