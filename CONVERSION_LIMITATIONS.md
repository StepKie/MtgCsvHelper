# Conversion Limitations

Cross-format conversion is not always lossless. Each platform encodes a different subset of card attributes — converting between two formats can silently drop or coerce data. This document enumerates the loss axes so you know what to expect *before* you trust a round-trip.

Per-format detail (with screenshots and round-trip notes) lives in [`MtgCsvHelper/Resources/SampleCsvs/Tests/SITE_BEHAVIOR.md`](MtgCsvHelper/Resources/SampleCsvs/Tests/SITE_BEHAVIOR.md). This doc is the user-facing summary.

## Format support matrix

| Format         | Read | Write | Conditions   | Languages | Etched finish | Variant foils  | Notes                                                                |
| -------------- | :--: | :---: | :----------: | :-------: | :-----------: | :------------: | -------------------------------------------------------------------- |
| **Moxfield**     | ✅   | ✅   | 6            | 11        | ✅            | —              | Excellent collapses to "Near Mint" on write                          |
| **Manabox**      | ✅   | ✅   | 7            | 11        | ✅            | —              | Corrects rarity, fills internal IDs                                  |
| **DragonShield** | ✅   | ✅   | 7            | 11        | ❌            | Rainbow / Double Rainbow / Gilded — collapsed to Foil | Cross-format importer is lossy (see below) |
| **TopDecked**    | ✅   | ✅   | 6            | 11        | ✅            | —              | TCGPlayer condition vocabulary                                       |
| **Deckbox**      | ✅   | ✅   | 6            | 11        | ⚠️ stored as Foil | —          | Collapses etched → foil on storage; uses internal edition aliases    |
| **Archidekt**    | ✅   | ✅   | 5            | 11        | ✅            | —              | TCGPlayer-standard 5 (NM/LP/MP/HP/D); Mint + Excellent → NM on write |
| **TCGPlayer**    | ✅   | ✅   | 6            | 11        | ❌            | —              | Paywalled (Level 4 Seller); fixture is synthetic                     |
| **MtgGoldfish**  | ✅   | ✅   | **0** (none) | **0** (none) | ✅          | —              | No Condition or Language columns at all — both fields lost           |
| **Cardmarket**   | ✅   | —    | 7            | 11        | ❌            | —              | Identifies cards by `idProduct` only; no Name/Set/Collector# columns |
| **CardKingdom**  | —    | ✅   | —            | —         | ❌            | —              | Write-only; cannot round-trip through this format                    |

## Loss axes

### 1. Finish: Etched → Foil collapse

Our internal model uses `bool? Foil`. Etched is parsed but stored as `Foil = true` (data loss point) and re-emitted as the format's regular Foil string on write — so an etched card round-tripped through any format is indistinguishable from a regular foil on output.

This collapse happens **on every write path** today. Formats that *natively* track etched (Moxfield, Manabox, TopDecked, Archidekt, MtgGoldfish) lose the distinction once it passes through our parser. A tri-state `Foil` enum is the planned fix; tracked as a follow-up.

**Worst case:** writing to a format that doesn't support Etched at all (DragonShield, TCGPlayer, CardKingdom, Cardmarket) — etched cards are silently promoted to "regular foil" twice over.

### 2. Condition: precision loss between 0/5/6/7-condition formats

Our internal model has 7 conditions: `Mint, NearMint, Excellent, Good, LightlyPlayed, Played, Poor`. Each format encodes a different subset, with these collapse rules on writes:

| Internal model    | Moxfield                | Manabox          | DragonShield  | TopDecked          | Deckbox                 | TCGPlayer          | Archidekt | Cardmarket | MtgGoldfish |
| ----------------- | ----------------------- | ---------------- | ------------- | ------------------ | ----------------------- | ------------------ | --------- | ---------- | ----------- |
| **Mint**          | `Mint`                  | `mint`           | `Mint`        | `mint`             | `Mint`                  | `Mint`             | **`NM`**  | `1`        | —           |
| **NearMint**      | `Near Mint`             | `near_mint`      | `NearMint`    | `near mint`        | `Near Mint`             | `Near Mint`        | `NM`      | `2`        | —           |
| **Excellent**     | **`Near Mint`**         | `excellent`      | `Excellent`   | **`near mint`**    | **`Near Mint`**         | **`Near Mint`**    | **`NM`**  | `3`        | —           |
| **Good**          | `Good (Lightly Played)` | `good`           | `Good`        | `slightly played`  | `Good (Lightly Played)` | `Lightly Played`   | `LP`      | `4`        | —           |
| **LightlyPlayed** | `Played`                | `light_played`   | `LightPlayed` | `moderately played`| `Heavily Played`        | `Moderately Played`| `MP`      | `5`        | —           |
| **Played**        | `Heavily Played`        | `played`         | `Played`      | `heavily played`   | `Played`                | `Heavily Played`   | `HP`      | `6`        | —           |
| **Poor**          | `Damaged`               | `poor`           | `Poor`        | `damaged`          | `Poor`                  | `Damaged`          | `D`       | `7`        | —           |

**Read-direction collapses to watch for:**

- **Archidekt → internal**: `NM` resolves to `NearMint` (first-match wins). A card originally written as `Mint` or `Excellent` re-reads as `NearMint`.
- **Moxfield / Deckbox / TopDecked / TCGPlayer → internal**: similarly resolves the shared `Near Mint` to `NearMint`; an originally-Excellent card is indistinguishable from NearMint after a Moxfield round-trip.
- **MtgGoldfish**: no Condition column at all. **Every card reads back as `UNKNOWN`.**

### 3. Language: MtgGoldfish drops it entirely

MtgGoldfish CSVs have no Language column. Round-tripping through MtgGoldfish loses *all* language information — Japanese / German / Russian cards come out indistinguishable from English on the way back in (defaulted to `null`).

All other formats support the same 11 Scryfall language codes (en, fr, de, es, it, zhs, ja, pt, ru, ko, zht), though each uses its own string vocabulary (e.g., Archidekt writes `EN`, Moxfield writes `English`, Cardmarket writes `1`).

### 4. DragonShield variant foils

DragonShield's `Printing` column emits more than three values: in addition to `Normal`/`Foil`/`Etched`, real exports contain `Rainbow Foil`, `Double Rainbow Foil`, and `Gilded Foil`. Our parser collapses all three to `Foil = true` (a hardcoded `FoilVariants` list in `FinishConverter`). The variant distinction is **always lost** when round-tripping through DragonShield via this tool.

### 5. Deckbox edition aliases

Deckbox uses internal Edition names that diverge from Scryfall for several sets:

| Scryfall name             | Deckbox name                   |
| ------------------------- | ------------------------------ |
| `Secret Lair Drop`        | `Secret Lair Drop Series`      |
| `Ultimate Box Topper`     | `Ultimate Masters: Box Toppers`|
| `<X> Tokens`              | `Extras: <X>`                  |

Writing to Deckbox without the right alias risks the card landing as "Unspecified Edition" — silent loss of printing context. Currently tracked as [#31](https://github.com/StepKie/MtgCsvHelper/issues/31); a writer-side alias table is the planned fix.

### 6. DFC name shapes

Double-faced cards have several layouts (transform, modal_dfc, adventure, split, meld). DragonShield specifically requires *short* names for `transform` and `modal_dfc` but *full* names (`A // B`) for `adventure` and `split`. Other formats are mixed. Our parser is layout-agnostic on read (uses the catalog's DFC index), but write-side determinism varies per format.

### 7. Cardmarket: idProduct-only model

Cardmarket CSV exports do not contain Name, Set, or Collector Number. Resolution happens via reverse-lookup of `idProduct` against Scryfall. Two failure modes:

- **Scryfall has no `cardmarket_id` for the printing** — e.g., some tokens (`Clue Token TMH2 #14`, `Treasure Token TCLB #17`) and the 30th Anniversary Edition (`30a`). Cardmarket *does* list these products, but Scryfall's data hasn't crosslinked them; the import surfaces a Warning per affected row and drops the card.
- **Cardmarket is read-only** — there is no writer. Cards cannot be exported back into Cardmarket format from this tool.

### 8. Cross-format importer quirks

Some platforms have *their own* moxfield-import path (DragonShield, Archidekt). These cross-format importers are observably lossy beyond what our own writers do:

- **DragonShield's Moxfield-importer** drops `Mint` and `Damaged` rows entirely; remaps `Good (Lightly Played)` → `Good`, `Heavily Played` → `Poor`.
- **Archidekt's Moxfield-importer** silently maps `Good (Lightly Played)` → `NM` (skipping LP entirely), in addition to the expected `Mint → NM` collapse.

These are external-platform behaviours, not bugs in our tool — but they affect the practical lossiness of any chain that includes a Moxfield-import step on those platforms.

### 9. Read-only and write-only formats

- **CardKingdom is write-only.** Round-trip through CardKingdom is impossible; the tool can emit CardKingdom CSVs but not parse them.
- **Cardmarket is read-only.** The tool can parse Cardmarket exports (with Scryfall reverse-lookup) but cannot write Cardmarket CSVs.

## Round-trip determinism by source format

The "least lossy" formats — those that come closest to round-tripping our 7-condition × 11-language × 3-finish internal model — are **Manabox** and **DragonShield** (both 7-condition, all languages, though DragonShield lacks Etched). **Archidekt**, **TopDecked**, and **TCGPlayer** lose condition precision (5 or 6 distinct values). **MtgGoldfish** loses both condition *and* language entirely. **CardKingdom** can't be round-tripped at all (write-only). **Cardmarket** can't be round-tripped at all (read-only).

If you need to preserve condition precision across multiple platforms, route your data **through Manabox or DragonShield** rather than the lossier formats.
