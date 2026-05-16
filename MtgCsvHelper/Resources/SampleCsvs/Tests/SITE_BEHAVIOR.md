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

## TopDecked

Status: 43/43 rows round-trip cleanly (May 2026). Full parity with Moxfield.

**Schema:**
- Header uses ALL-CAPS-with-selectively-quoted columns: `QUANTITY,"NAME",SETCODE,"SETNAME","COLLECTOR NUMBER",FINISH,PRICE,RARITY,ID,ACQUIRED DATE,ACQUIRED PRICE,LANG,PRICE SALE,SIGNING,ALTERATION,CONDITION,NOTES,TAGS`.
- Set codes lowercase (`ltr`, `m11`, `tmh2`).
- Set names with embedded colons quoted (`"The Lord of the Rings: Tales of Middle-earth"`).
- Date format: `Fri May 15 2026 16:43:29 GMT+0200 (Central European Summer Time)` — verbose locale-specific timestamp.
- Has `ID` column = Scryfall UUID (filled by TopDecked on import).
- Rich extra columns (`SIGNING`, `ALTERATION`, `NOTES`, `TAGS`) we don't currently model — present as empty cells.

**Import path that worked:**
- **Direct content paste** via TopDecked's "paste data" feature succeeded after some encoding troubleshooting (see Quirks).

**Enriches on import:**
- `ID` (Scryfall UUID) filled in for every recognized printing.
- `PRICE` filled in with current EUR market price (e.g. `€2521.66` for serialized Aragorn).
- `RARITY` corrected against Scryfall (submitted Disciplined Duelist as `rare` → re-exported as `uncommon`, the actual SNC rarity).
- `ACQUIRED DATE` set to import-time timestamp.

**Normalizes:**
- **Auto-corrects invalid finish/printing combos**: submitted `Demonic Tutor CMM #509 foil` → corrected to `etched` because CMM has that printing as etched-only. Per-row diagnostic shown: `"Printing does not exist for the requested finish: 'foil'. Changed to 'etched'"`.
- For printings with multiple finishes available (e.g. STA #27 has `{nonfoil, foil, etched}`), TopDecked respects the user's chosen finish without correction (`foil` stayed `foil`).

**Validation quirks:**
- `Finish` column is binary-ish: `nonfoil`/`foil`/`etched`. **Does not track Rainbow Foil / Double Rainbow Foil / Gilded Foil** distinctions that DragonShield emits — variant foils all collapse to `foil`. Our `bool? Foil` model matches.
- Gap fixed during this work: `appsettings.json` had `Etched: null` for TOPDECKED — set to `"etched"` after the round-trip proved TopDecked emits it.
- Per-row import diagnostics are richer than DragonShield's silent failures — clear messages like "Printing does not exist for the requested finish" or "No matching cards found".

**Encoding quirk:**
- TopDecked's **paste-and-import** UI clobbers UTF-8 → reads as Latin-1, mangling `û` → `Ã»` and adding garbage prefix bytes if a BOM is present. **Workaround**: use "Direct content paste" feature (whatever that means in TopDecked's UI — Stephan found it). File upload would presumably also work; not tested.

---

## Deckbox

Status: 42 rows (May 2026). All site-blessed via Deckbox's **Manabox-format import** (1 row short of Moxfield's 43 because Manabox-source had already lost `Damaged`). Deckbox's native CSV import path is stricter (see below) — the Manabox-format path was the way to import a richer card set.

**Schema:**
- `Edition` is full name, `Edition Code` is the abbreviation — split across two columns.
- Set codes lowercase (`ltr`, `mid`, `sld`).
- Tokens use `Extras: <set>` prefix on the Edition name with a Deckbox-internal `ex_NNN` set code (e.g. `Clue` token in Modern Horizons 2 → `Extras: Modern Horizons 2` / `ex_127` / `14`). The `ex_NNN` codes are Deckbox-internal — not derivable from Scryfall.
- `CurrencySymbol: Start` — prices like `$12.00` (dollar prefix).
- 19 columns including misc binary flags (`Signed`, `Artist Proof`, `Altered Art`, `Misprint`, `Promo`, `Textless`) we don't model.

**Enriches on import:**
- `Printing Id` (Deckbox-internal numeric ID).
- `Cost` (mana cost like `{R}{G}{W}{U}`).
- `Rarity` (corrected against Deckbox's database).
- `Price` (current market price).
- `TcgPlayer ID`, `Scryfall ID`.

**Normalizes:**
- **Collapses etched → foil** on storage. Deckbox doesn't track etched as a separate finish; `Demonic Tutor STA #27 etched` becomes `Demonic Tutor STA #27 foil` in the export. Our `bool? Foil` model matches this collapse on the write side.
- **Strips letter prefixes from collector numbers**: `puma U8` stored as `puma 8`. Lossy — `U8` and `8` would technically be different printings per Scryfall but Deckbox treats them as the same; importing both stacks the quantity.
- **Uses Deckbox-canonical Edition names** that diverge from Scryfall for some sets:
  - Scryfall `Secret Lair Drop` → Deckbox `Secret Lair Drop Series`
  - Scryfall `Ultimate Box Topper` → Deckbox `Ultimate Masters: Box Toppers`
  - Scryfall `<X> Tokens` → Deckbox `Extras: <X>` (with the `Tokens` suffix dropped)

**Native CSV import vs Manabox-format import** (Deckbox supports both):
- **Native CSV import is strict** — Edition name and Edition Code must match Deckbox's canonical pairing for the card to resolve to a real printing. If they don't match, Deckbox may either (a) reject the row, (b) accept it as "Unspecified Edition" (loses the printing context). Empirically determined:
  - Blank Edition + lowercase Scryfall set code: **auto-resolves only for some sets** (`sld`, `puma`, `tmh2`, `tclb` work — Deckbox has internal code-to-name mapping for these). For other codes (`sta`, `cmm`) the rows land as "Unspecified Edition" — bad outcome.
  - Wrong Edition name + correct code: rejected (we proved this with `Modern Horizons 2 Tokens` for the token set instead of the Deckbox-canonical `Extras: Modern Horizons 2`).
  - `etched` value in the `Foil` column: **rejected** by native import. Must use `foil` (or leave blank).
- **Manabox-format import is more permissive** — accepts everything in our 42-row fixture, including the cards above that failed native import. Likely uses richer matching (Scryfall ID or similar).

**Writer-modeling implications** for `MtgCsvHelper`'s `DECKBOX` writer (follow-up issue):
1. Emit lowercase set codes (already mostly handled — depends on source).
2. Emit `foil` for etched cards (already handled by `bool? Foil` collapse in `FinishConverter.ConvertToString`).
3. **Need Set Name aliasing** for divergent sets (Secret Lair, Box Toppers, token sets) — leaving Edition blank only auto-resolves for some codes; missing the alias leaves cards in "Unspecified Edition". Small hand-curated mapping table.
4. Letter-prefix collector numbers (`U8`) pass through cleanly; Deckbox normalizes server-side. No need to strip in our writer.

---

## TCGPlayer

Status: 43 rows, **synthetic only** (not site-blessed). TCGPlayer's CSV import/export is paywalled — restricted to **Level 4 Seller** accounts. Round-trip blessing isn't feasible without paid seller access.

**Format reference**: real TCGPlayer export available at `Resources/SampleCsvs/Collection/tcgplayer-collection.csv` (500+ rows from a real seller account, different cards than our 43-row test set).

**Schema** (from the real export):
- 12 columns: `Quantity,Name,Simple Name,Set,Card Number,Set Code,Printing,Condition,Language,Rarity,Product ID,SKU`.
- `Simple Name` is a sanitized version of `Name` (e.g. `Lim-Dûl's Vault` → `Lim-Duls Vault`, `Aragorn, the Uniter` → `Aragorn the Uniter`).
- Set codes UPPERCASE.
- `Product ID` and `SKU` columns — TCGPlayer-internal identifiers, enriched on import.
- No `Purchase Price` column.
- `Etched: null` in our appsettings — unverified without round-trip access. Synthetic draft uses `Foil` for etched DT printings.

---

## MtgGoldfish

Status: 29 rows, **synthetic only** (not site-blessed). MtgGoldfish's CSV import is paywalled — `"CSV Import is only available for Premium members"`. Round-trip blessing isn't feasible without a Premium account.

**Format reference**: real MtgGoldfish exports available at `Resources/SampleCsvs/Collection/mtggoldfish-collection.csv` and `mtggoldfish-from-mtgarena.csv` (different cards than our test set).

**Schema notes:**
- 8 columns: `Card,Set ID,Set Name,Quantity,Foil,Variation,Collector Number,Scryfall ID`.
- **No `Condition` or `Language` columns** at all → reference-collection has 29 rows (synthetic) vs other formats' 42-43 (no per-condition or per-language enum coverage rows possible).
- `Etched: "foil_etched"` in our appsettings — distinct value, unverified without round-trip access.
- `Scryfall ID` column present — MtgGoldfish probably uses it for matching on import.
- `Etched: "foil_etched"` (not `etched`). The draft uses `foil_etched` for DT STA + CMM.
- `Variation` column purpose unclear; left empty.
- `Scryfall ID` column present but unfilled in the draft.

---

## Archidekt

Status: 43/43 rows round-trip cleanly (May 2026). Full parity with Moxfield. Closes #41.

**Schema:**
- 13 columns: `Quantity,Name,Finish,Condition,Date Added,Language,Purchase Price,Tags,Edition Name,Edition Code,Multiverse Id,Scryfall ID,Collector Number`.
- Set codes lowercase (`ltr`, `mid`, `tmh2`).
- DFCs use full `A // B` names.
- ISO date format (`2026-05-15`).

**Enriches on import:**
- `Multiverse Id` (Scryfall's wizards-of-the-coast ID — `617022` etc.) — `0` when the printing has no multiverse ID (foils, alternate art, tokens).
- `Scryfall ID` (the UUID) populated for every row.

**Foil values**: `Normal` / `Foil` / `Etched` — supports all three Scryfall finishes cleanly.

**Condition vocabulary**: only 6 strings (`M, NM, LP, MP, HP, D`) — no separate Excellent value. Our 7-enum model collapses `Excellent` → `NM` on writes (same pattern as Moxfield). The map is one-way: an internal `Excellent` written as `"NM"` round-trips back as `NearMint`, since `CardConditionConverter` resolves `"NM"` to the first match (`NearMint`). Intentional — Archidekt has no Excellent tier, and `appsettings.json` Archidekt > Condition encodes both `NearMint` and `Excellent` as `"NM"` deliberately.

**Language vocabulary** uses Archidekt-specific 2-letter codes that diverge from common conventions for some entries:
- `EN, DE, FR, IT, ES, PT, RU` (standard ISO)
- `JP` not `JA` (Japanese)
- `KR` not `KO` (Korean)
- `CS` for Chinese Simplified (not `zhs` or `zh-CN`)
- `CT` for Chinese Traditional (not `zht` or `zh-TW`)

**No native finish or set-name aliasing quirks** observed — Archidekt accepted everything from the 42-row Manabox source plus normalized its own additions cleanly. Cross-format diversity (Demonic Tutor across 8 sets, variant-finish cards, accent + apostrophe names) all imported without errors or normalization.

---

## Cardmarket (read-only, by `idProduct`)

Status: 40 rows (May 2026). Generated from `moxfield-reference-collection.csv` via `tools/MtgCsvHelper.RefreshReferenceData -- cardmarket-fixture`. The generator parses the moxfield reference, looks up each printing's `cardmarket_id` in the catalog, and emits a Cardmarket-shaped row. 3 of the 43 moxfield rows had no `cardmarket_id` and were skipped: Clue token (tmh2 #14), Treasure token (tclb #17), Demonic Tutor 30th Anniversary (30a #101) — Cardmarket doesn't list those products.

**Schema:** 14-column semicolon-delimited CSV from cardmarket.com's "Manage Stock" export.
`idProduct;groupCount;price;idLanguage;condition;isFoil;isSigned;isAltered;isPlayset;isReverseHolo;isFirstEd;isFullArt;isUberRare;isWithDie`

Card resolution is `idProduct` → Scryfall reverse-lookup (`/cards/cardmarket/{id}`); Name, Set, Collector Number are absent and filled in by enrichment. Different language versions of the same printing share an `idProduct` — language/condition/foil are per-listing attributes, not part of the product key.

**Field mapping verifications:**

- `idLanguage` (1–11) — verified against the MKM REST API v2.0 docs ([`API_2.0:Stock`](https://api.cardmarket.com/ws/documentation/API_2.0:Stock)) and the `pymkm` wrapper. Mapping: `1=en, 2=fr, 3=de, 4=es, 5=it, 6=zhs, 7=ja, 8=pt, 9=ru, 10=ko, 11=zht`.
- `condition` (integers 1–7 in the CSV export) — **not officially documented**. The REST API itself uses two-letter string codes (`MT, NM, EX, GD, LP, PL, PO`); the website's stock CSV switches to integers, confirmed by [`demogorgon1/mkmcsv`](https://github.com/demogorgon1/mkmcsv) ("Cardmarket CSVs use integers; manual shipment lists use abbreviated strings"). The integer mapping is the natural ordering of the string codes (`1=MT, 2=NM, …, 7=PO`); empirically validated against a real 113-row export (commit `af36b6c1`).
- `isFoil` — `"1"` for foil, empty for non-foil. Cardmarket doesn't track Etched as a separate finish, so etched printings collapse to `isFoil=1` on write (same lossy collapse as our `bool? Foil` model).

**Coverage in the generated fixture:**

- All 11 `idLanguage` values via the 11 Lightning Bolt M11 #149 language rows.
- Conditions `1, 2, 4, 5, 6, 7` (six of seven). `3=Excellent` is structurally missing: moxfield's `Excellent` collapses to `"Near Mint"` in its appsettings, so no moxfield row produces a parsed `CardCondition.EXCELLENT`. Synthetic field-fidelity coverage of `condition=3` lives in `cardmarket-field-fidelity.csv`.
- `isFoil=1` exercised by 8 rows (Aragorn foil, Lightning Bolt foils, Demonic Tutor variants, Disciplined Duelist).

---

## CardKingdom (write-only)

Status: no reference-collection — write-only format.

The CardKingdom CSV format is what our writer emits; there's no import path. Tests for CardKingdom go through `WriteCollectionCsv` round-trips, not the fixture pattern.
