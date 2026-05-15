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

**Validation quirks:**
- Etched is supported in the Foil column (`etched` string). Preserved on round-trip. Our `appsettings.json` encodes this; etched data still collapses to `bool Foil` in our internal model — follow-up.
- The `Folder Name` column in the collection-export form is critical for grouping; absent from binder exports.

---

## Manabox

Status: 40/41 rows round-trip cleanly (May 2026). 1 synthetic row added back (Valki — see below).

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

**Validation quirks:**
- The `Foil` column is a clean 3-value enum: `normal` / `foil` / `etched`. Each row carries exactly one. In our 40-row reexport: 33 `normal`, 4 `foil` (LB M11 foil, LB SLD, DT SLD, DT PUMA), 2 `etched` (DT STA, DT CMM).
- Gap fixed during this work: `appsettings.json` had `Etched: null` for MANABOX — the `normal` and `foil` strings were already configured, but the parser would reject `etched` rows. Updated to `"Etched": "etched"` after the reexport proved Manabox emits all three.

**Carry-overs / current state of `Tests/manabox-reference-collection.csv`:**
- 40 rows are Manabox-blessed (real reexport).
- The Valki row is synthetic (Manabox rejected it due to the quoting bug). Re-import + re-export pending if we want it fully blessed.

---

## DragonShield (not yet round-tripped)

Status: draft generated, not yet imported.

**Notes from `appsettings.json` + the existing `Samples/dragonshield-sample.csv`:**
- First-line `"sep=,"` marker — handled by the parser via `CheckIfFirstLineCanBeIgnored`.
- `ShortNames: true` — DFC names emit as front-face-only (e.g. `Brazen Borrower` not `Brazen Borrower // Petty Theft`). The draft uses short names accordingly.
- `EncodeToken: true` — token names get a ` Token` suffix on emit (`Clue` → `Clue Token`, `Treasure` → `Treasure Token`).
- `Etched: null` — DragonShield's CSV format may or may not support etched. The draft uses `Foil` (not `etched`) for the etched DT printings; if DragonShield distinguishes etched, the re-export will reveal it.

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
