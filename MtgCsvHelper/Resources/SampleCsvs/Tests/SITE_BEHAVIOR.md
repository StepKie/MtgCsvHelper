# Site Behavior Log

How each format's site behaves on import/export — what it drops, what it enriches, what it normalizes, validation quirks. Filled in as we round-trip the per-format `reference-collection.csv` through each site.

Conventions:
- **Drops on export**: columns present on import that the site does not re-emit.
- **Enriches on import**: data the site fills in or looks up from its own database.
- **Normalizes**: values the site rewrites (set names, condition strings, rounding).
- **Rejects**: rows the site refuses on import (validation rules).
- **Gaps**: things the site supports that our `appsettings.json` doesn't yet model (or vice versa).

---

## Moxfield

Status: 41/41 rows round-trip cleanly (May 2026).

**Schema:** binder and collection exports differ.
- Binder export (the `moxfield_haves_*.csv` form): `Count,Tradelist Count,Name,Edition,Condition,Language,Foil,Tags,Last Modified,Collector Number,Alter,Proxy,Purchase Price`.
- Collection export (`moxfield-collection.csv`): different column set including `Folder Name`, `Trade Quantity`, `Set Name`, `AVG`/`LOW`/`TREND`. Importantly: uses `Condition = NM` (abbreviation), not `Near Mint`. Our `appsettings.json` only encodes the binder form; this is why `CardConditionConverter` stays lenient (follow-up: per-enum aliases).
- `Tradelist Count` is an independent value from `Count` — it tracks "how many of this row you're willing to trade", separate from "how many you own". Both are first-class columns.

**Enriches on import:**
- `Last Modified` timestamp — set on every row regardless of input.
- Trims insignificant trailing zeros on `Purchase Price` (`2.00` → `2`, sometimes).
- `Alter` and `Proxy` always export as explicit `False`/`False` strings (never blank), even if those columns were blank on import.

**Normalizes:**
- Row order on export is not input order and not alphabetic by any single field — appears insertion-order-with-shuffle. Don't rely on order in tests.
- Auto-resolves wrong collector numbers to a valid printing during import (fuzzy match by Name + Set). Means a wrong number imports without error but doesn't round-trip the way you'd expect. This is the strongest argument for the catalog-validation test (#60 work) — Moxfield's import is too forgiving to catch real data corruption. Our catalog test does catch it.

**Rejects:**
- Cards Moxfield's database doesn't know (e.g. token in wrong set). Error message format: `Could not find card named "<Name>" in edition "<set>". on line <N>`.
- **Minimum-info imports** with blank `Name` (just `Edition` + `Collector Number`) are rejected — every row produces `No card name found. on line <N>`. Moxfield's resolution is Name-anchored; (Set + Collector#) alone isn't enough, contrary to what its fuzzy-matching tolerance might suggest. Same posture as DragonShield.

**Validation quirks:**
- Etched is supported in the Foil column (`etched` string). Preserved on round-trip. Our `appsettings.json` encodes this; etched data still collapses to `bool Foil` in our internal model — follow-up.
- The `Folder Name` column in the collection-export form is critical for grouping; absent from binder exports.

---

## Manabox

Status: 42 rows (May 2026). Latest round-trip imported our 43-row Moxfield reference-collection via Manabox's Moxfield-format importer; Manabox dropped 1 row (`Lightning Bolt M11 Damaged` — Manabox's Moxfield-importer doesn't accept `Damaged` as a Condition value even though it's a real Moxfield string). Re-exported as 42 site-blessed rows. 1 row short of Moxfield's 43 — same enum POOR gap as DragonShield. Earlier exploratory session had used a Manabox-native import path with all 6 Moxfield conditions accepted; the current cross-format path has the gap.

**Schema:** import accepts extra columns the export doesn't emit.
- Import accepts: `Binder Name,Binder Type,Name,Set code,Set name,Collector number,Foil,Rarity,Quantity,ManaBox ID,Scryfall ID,Purchase price,Misprint,Altered,Condition,Language,Purchase price currency`.
- Export emits a slimmer 15-column form: drops `Binder Name` and `Binder Type` (these affect storage on Manabox's side but aren't round-tripped through the CSV).

**Enriches on import:**
- `ManaBox ID` and `Scryfall ID` are looked up and filled in for every recognized printing.
- `Purchase price`: if blank on input, Manabox fills in its current market price (e.g. Recruiter of the Guard PLST CN2-22 → `5.68 USD`).
- `Rarity`: corrected against Scryfall data (e.g. submitted "mythic" for DT UMA → re-exported as `rare`, the actual UMA rarity).

**Normalizes:**
- Set names dropped of cross-set-prefix terms (e.g. submitted `Commander Legends: Battle for Baldur's Gate Tokens` → re-exported as `Battle for Baldur's Gate Tokens`).
- Price decimals: `12.00` → `12.0`, `1.50` → `1.5` (one decimal place when applicable).

**Rejects:**
- Rows where embedded commas aren't quoted. Our synthetic Valki row `Valki, God of Lies // Tibalt, Cosmic Impostor` was unquoted in the original draft → split into multiple cells → "Line N incorrect: Missing columns". Always quote names that contain commas.
- **Minimum-info imports** with blank `Name` (just `Set code` + `Collector number`) are rejected. Manabox's resolution requires Name, same posture as Moxfield and DragonShield.

**Validation quirks:**
- The `Foil` column is a clean 3-value enum: `normal` / `foil` / `etched`. Each row carries exactly one. In our 40-row reexport: 33 `normal`, 4 `foil` (LB M11 foil, LB SLD, DT SLD, DT PUMA), 2 `etched` (DT STA, DT CMM).
- Gap fixed during this work: `appsettings.json` had `Etched: null` for MANABOX — the `normal` and `foil` strings were already configured, but the parser would reject `etched` rows. Updated to `"Etched": "etched"` after the reexport proved Manabox emits all three.

**Carry-overs / current state of `Tests/manabox-reference-collection.csv`:**
- 40 rows are Manabox-blessed (real reexport).
- The Valki row is synthetic (Manabox rejected it due to the quoting bug). Re-import + re-export pending if we want it fully blessed.

---

## DragonShield

Status: 42 rows (May 2026). Path: imported our 43-row Moxfield reference-collection via DragonShield's **Moxfield-format import** (DragonShield supports cross-format imports), re-exported as DragonShield → 41 site-blessed rows (DragonShield's Moxfield-importer dropped Mint + Damaged). Added 1 synthetic row (`Lightning Bolt M11 #149 Mint`) to restore enum-MINT coverage. The enum-POOR coverage already exists in the file as a side-effect of DragonShield's Heavily Played → Poor mismapping (so we didn't add a synthetic Poor — would have duplicated the string). 1 row short of Moxfield's 43 because Damaged is unrepresentable as a "clean" DragonShield row without a manual UI add. Confirmed the file round-trips through DragonShield's **native** importer faithfully.

DragonShield's native importer is strict — synthetic drafts that diverge from the exact export format (`"sep=,"`, 15 columns including `LOW/MID/MARKET`, CRLF line endings, layout-aware DFC name shapes) get rejected silently with a generic "please check structure" message. The Moxfield-import path is the easier entry point for building from scratch.

**Schema:**
- First-line `"sep=,"` marker — required on real exports; handled by our parser via `CheckIfFirstLineCanBeIgnored`.
- 15 columns: `Folder Name, Quantity, Trade Quantity, Card Name, Set Code, Set Name, Card Number, Condition, Printing, Language, Price Bought, Date Bought, LOW, MID, MARKET`.
  - **`LOW/MID/MARKET`** are the current price-tracking columns. (Older DragonShield exports used `AVG/LOW/TREND` — schema evolved.)
  - The published "format guide" sample uses a slimmer 12-column form (no `"sep=,"`, no price-tracking columns) — that's a template, **not** the real export shape.
- ASCII line endings: **CRLF required**.

**DFC name handling varies by layout:**
- `transform` (Delver of Secrets → `Delver of Secrets`) → short name.
- `modal_dfc` (Valki → `Valki, God of Lies`) → short name.
- `adventure` (Brazen Borrower // Petty Theft) → **full** name.
- `split` (Fire // Ice) → **full** name.

So DragonShield's `ShortNames: true` config setting only applies to a subset of multi-face layouts. Our parser is layout-agnostic on read (it handles both short and full via `CardNameConverter` + catalog DFC index), so this works in practice — but if/when we extend `appsettings.json` for nuanced write-side rules, this distinction matters.

**Enriches on import:**
- `LOW/MID/MARKET` price columns populated from DragonShield's price database.
- `Date Bought` set to import-day even if input was blank.

**Normalizes:**
- **The List entries get demangled** to original printings. We submitted `Demonic Tutor PLST DDC-49` → re-exported as `Demonic Tutor DVD #49` (Duel Decks: Divine vs. Demonic). Submitted `Recruiter of the Guard PLST CN2-22` → re-exported as `Recruiter of the Guard CN2 #22` (Conspiracy: Take the Crown). DragonShield treats PLST as a redirection and unwinds it.
- **Variant foil treatments**: DragonShield's Printing column emits more than 3 values. Observed: `Normal`, `Foil`, `Etched`, `Rainbow Foil`, `Double Rainbow Foil`, `Gilded Foil`. Our model collapses them all to `bool? Foil` (true for any foil-like; false for normal). Hardcoded fallback in `FinishConverter` keeps the parser from rejecting these strings. Follow-up: config-level aliases per format, and possibly tri-state finish in the model.
- **Resets `Price Bought` to `0.00`** on Moxfield-format import.

**Rejects:**
- **Minimum-info imports** with blank `Card Name` + `Set Name` (just `Set Code` + `Card Number`) are rejected. DragonShield requires the name fields to be populated; it doesn't deduce the card from `(Set Code, Card Number)` alone like Moxfield's fuzzy matcher does.
- Diverging synthetic drafts fail silently with a generic "please check structure" message — DragonShield doesn't report specific row errors on native import. Round-tripping through Moxfield-format import gives more diagnostics (errors are listed per row).

**Moxfield-format importer in DragonShield is buggy:**
The cross-format import path that worked has gaps. Observed mapping behavior from Moxfield → DragonShield:

| Moxfield string | Expected DS mapping | Actual |
|---|---|---|
| `Mint` | `Mint` | **DROPPED** (row ignored) |
| `Near Mint` | `NearMint` | `NearMint` ✓ |
| `Good (Lightly Played)` | `LightPlayed` (literal name match!) | **`Good`** (skipped LightPlayed in middle) |
| `Played` | `Played` | `Played` ✓ |
| `Heavily Played` | `Played` or new value | **`Poor`** (jumped to worst) |
| `Damaged` | `Poor` | **DROPPED** (row ignored) |

So DragonShield's Moxfield-importer maps only 4 of Moxfield's 6 conditions, with at least one wrong middle mapping. Our DragonShield reference-collection therefore has only 4 of DS's 7 condition values represented (NearMint, Good, Played, Poor). The other 3 (Mint, Excellent, LightPlayed) would need manual UI entry to cover.

---

## TopDecked (not yet round-tripped)

Status: draft generated, not yet imported.

**Notes:**
- Header uses ALL-CAPS-with-quotes (`QUANTITY,"NAME",SETCODE,"SETNAME",...`). Some columns quoted, others not.
- Set codes lowercase (`mid`, `tmh2`).
- `Etched: null` — same posture as DragonShield: draft uses `foil` for the etched DT printings until proven otherwise.
- Lots of columns we don't currently model (`RARITY`, `ID` (Scryfall UUID), `ACQUIRED DATE`, `SIGNING`, `ALTERATION`, `NOTES`, `TAGS`) — present in the draft as empty cells.

---

## Deckbox (not yet round-tripped)

Status: draft generated, not yet imported.

**Notes:**
- `Edition` is full name, `Edition Code` is the abbreviation — split across two columns.
- Token printings live in `Extras: <set name>` set names (per the existing sample), not the main set. The draft uses the canonical Scryfall set name (`Modern Horizons 2 Tokens`); Deckbox may normalize this.
- `Etched: "etched"` is configured. Draft uses `etched` for DT STA + CMM.
- `CurrencySymbol: Start` — prices like `$0.20`.

---

## TCGPlayer (not yet round-tripped)

Status: draft generated, not yet imported.

**Notes:**
- `Simple Name` is a separate column from `Name` (e.g. `Lim-Dûl's Vault` → `Lim-Duls Vault` simple name; `Aragorn, the Uniter` → `Aragorn the Uniter`).
- `Etched: null` — same posture: draft uses `Foil` (not `etched`) for the etched DT printings.
- No `Purchase Price` column in this format.
- `Product ID` and `SKU` columns we don't fill in — TCGPlayer likely enriches on import.

---

## MtgGoldfish (not yet round-tripped)

Status: draft generated, not yet imported.

**Notes:**
- No `Condition` or `Language` columns at all in the export schema → reference-collection has 28 rows instead of 41 (no enum-coverage rows for those fields).
- `Etched: "foil_etched"` (not `etched`). The draft uses `foil_etched` for DT STA + CMM.
- `Variation` column purpose unclear; left empty.
- `Scryfall ID` column present but unfilled in the draft.

---

## Cardmarket (read-only, by `idProduct`)

Status: not approached — different shape from the per-format pattern.

Cardmarket imports cards by `idProduct` (cardmarket_id, an integer) plus per-row condition, language, finish — not by Name + Set + Collector Number. A reference-collection for Cardmarket would need real `idProduct` values looked up from the catalog. Defer to a follow-up.

---

## CardKingdom (write-only)

Status: no reference-collection — write-only format.

The CardKingdom CSV format is what our writer emits; there's no import path. Tests for CardKingdom go through `WriteCollectionCsv` round-trips, not the fixture pattern.
