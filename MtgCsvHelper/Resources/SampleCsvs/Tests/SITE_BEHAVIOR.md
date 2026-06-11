# Site Behavior Log

How each format's site behaves on import/export — what it drops, what it enriches, what it normalizes, validation quirks. Filled in as we round-trip the per-format `reference-collection.csv` through each site.

> **For the user-facing summary of cross-format data loss, see [`/CONVERSION_LIMITATIONS.md`](../../../../CONVERSION_LIMITATIONS.md) at the repo root.** This file is the per-format operational reference behind that summary.

Conventions:
- **Drops on export**: columns present on import that the site does not re-emit.
- **Enriches on import**: data the site fills in or looks up from its own database.
- **Normalizes**: values the site rewrites (set names, condition strings, rounding).
- **Rejects**: rows the site refuses on import (validation rules).
- **Gaps**: things the site supports that our `appsettings.json` doesn't yet model (or vice versa).

---

## Moxfield

Status: 41/41 rows round-trip cleanly (May 2026). **June 2026:** the 29-row reference set round-trips cleanly — borderless (`ltr 433`), guild kit (`gk2 #1`, resolved by collector number), The List (`plst` DDC-49), etched (preserved), and all 11 languages survived. The guild-kit + The List cases that DragonShield mangles resolve correctly here.

**Schema:** binder and collection exports differ.
- Binder export (the `moxfield_haves_*.csv` form): `Count,Tradelist Count,Name,Edition,Condition,Language,Foil,Tags,Last Modified,Collector Number,Alter,Proxy,Purchase Price`.
- Historical note: the 2022-era `moxfield-collection.csv` fixture (deleted in the Dragon Shield 12-column rework) carried shorthand conditions (`NM`/`LP`/`D`) in a Moxfield-shaped header with Dragon Shield columns (`Folder Name`, `AVG`/`LOW`/`TREND`) — most likely Dragon Shield's Moxfield-format export, not Moxfield's own. Neither site emits that shorthand as of June 2026 (verified by fresh exports of both Moxfield forms); `CardConditionConverter` is strict and rejects it.
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

**Tokens (verified by round-trip, June 2026):**
- Import wants plain Scryfall names with the Scryfall token-set code: `Beast` / `tmh2` / #9, `Clue` / `tmh2` / #15, `Morph` / `tktk` / #11 all import cleanly.
- Decorated names are rejected: `Beast Token`, `Clue Token`, `Morph Creature` each produce `Could not find card named "<Name>" in edition "<set>"`.
- Re-export preserves plain names, lowercase set codes, and collector numbers exactly — so the MOXFIELD writer must emit plain names (`EncodeToken` stays unset; only DragonShield decorates).

---

## Manabox

Status: 42 rows (May 2026). Latest round-trip imported our 43-row Moxfield reference-collection via Manabox's Moxfield-format importer; Manabox dropped 1 row (`Lightning Bolt M11 Damaged` — Manabox's Moxfield-importer doesn't accept `Damaged` as a Condition value even though it's a real Moxfield string). Re-exported as 42 site-blessed rows. 1 row short of Moxfield's 43 — same enum POOR gap as DragonShield. Earlier exploratory session had used a Manabox-native import path with all 6 Moxfield conditions accepted; the current cross-format path has the gap. **June 2026:** the 29-row reference set imported via Manabox's **native** importer round-trips cleanly — 29/29, all 6 conditions (`mint`…`poor`), all 11 languages (incl. Spanish), etched preserved, borderless → exact Scryfall ID, guild kit `gk2 #1`, The List `plst`. (The old "Damaged dropped" was the Moxfield *cross-import* path; the native path keeps everything.)

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
- **Variant foil treatments**: DragonShield's Printing column emits an open-ended set of values beyond the basic 3. Observed: `Normal`, `Foil`, `Etched`, `Rainbow Foil`, `Double Rainbow Foil`, `Gilded Foil`, `Surge Foil`, `Step and Compleat Foil`. New treatments keep appearing as WotC invents them, so `FinishConverter` matches any value containing the word `foil` (case-insensitive) rather than an allowlist that goes stale (#102). Our model still collapses every foil-like value to `bool? Foil` (true for any foil; false for normal). Follow-up: possibly tri-state finish in the model.
- **Ravnica Guild Kit set codes**: DragonShield emits proprietary per-guild codes — `GK1_<GUILD>` (Guilds of Ravnica, e.g. `GK1_DIMIR`) and `GK2_<GUILD>` (Ravnica Allegiance, e.g. `GK2_AZORIU`, `GK2_ORZHOV`, `GK2_SIMIC`, `GK2_GRUUL`, `GK2_RAKDOS`). The guild suffix is cosmetic (truncated to ≤6 chars: `azorius` → `AZORIU`) and the collector numbers are shared across the kit, so the Scryfall set is a single `gk1`/`gk2` (`GK2_AZORIU #2` == `gk2 #2`, Azorius Herald). `DragonShieldCodeReadConverter` collapses `GK[12]_*` to `gk1`/`gk2` on read (#102).
- **Resets `Price Bought` to `0.00`** on Moxfield-format import.
- **Simplified Chinese needs the full string**: DragonShield's `zhs` language value is `Simplified Chinese` (the parallel to `Traditional Chinese`). Our appsettings emitted the bare `Chinese`, which DragonShield didn't recognize — it silently filed those rows as English (no error, quantities merged into the English row). Confirmed by a manual round-trip (June 2026) and corrected to `Simplified Chinese`.

**Rejects (on *our* import of DragonShield exports):**
- **Minimum-info imports** with blank `Card Name` + `Set Name` (just `Set Code` + `Card Number`) are rejected. DragonShield requires the name fields to be populated; it doesn't deduce the card from `(Set Code, Card Number)` alone like Moxfield's fuzzy matcher does.
- Diverging synthetic drafts fail silently with a generic "please check structure" message — DragonShield doesn't report specific row errors on native import. Round-tripping through Moxfield-format import gives more diagnostics (errors are listed per row).
- **Regional / foreign-language set codes** like `LEGI` (Legends Italian) don't map to a Scryfall code and fail catalog validation (row dropped). Foreign-language card names also fail name-matching against our English-only catalog. Tracked in #103.

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

**DragonShield resolves printings by Set *Name*, not Set Code (export round-trip, June 2026):**
DragonShield's CSV importer keys on the `Set Name` (with Card Name + Card Number); it ignores the `Set Code` we send and re-derives its own on export. A full canonical round-trip of the special-product reference rows:

| We exported (Set Name) | DragonShield stored | Outcome |
|---|---|---|
| Mardu Outrider / Mystery Booster 2 #1 | Mystery Booster 2 #1 | ✓ set name matches → resolves |
| Viscera Seer / Secret Lair Drop #VS | Secret Lair Drop #VS | ✓ |
| Ral's Vanguard / Mystery Booster Playtest Cards #1 | Mystery Booster Playtest Cards #1 | ✓ |
| Demonic Tutor / The List #DDC-49 | Duel Decks Anthology `DVD` #49 | ⚠ PLST unwound to original printing (see Normalizes) |
| Isperia, Supreme Judge / **RNA Guild Kit** #1 | Return to Ravnica `RTR` #171 | ✗ set name unknown → name-matched to wrong edition |

MB2/SLD/CMB1 resolve because their canonical set names match DragonShield's; the guild kit fails because Scryfall's `RNA Guild Kit` ≠ DragonShield's per-guild `Guild Kit: Azorius`. Two targeted Isperia imports isolate the lever:

| We sent (Set Code / Set Name / #) | DragonShield stored | |
|---|---|---|
| `GK2_AZORIU` / `RNA Guild Kit` / 1 | Return to Ravnica #171 | ✗ wrong set name |
| `GK2_AZORIU` / `Guild Kit: Azorius` / 1 | Guild Kit: Azorius #1 | ✓ |
| `GK2` / `Guild Kit: Azorius` / 1 | Guild Kit: Azorius #1 (DS re-stamped its own `GK2_AZORIU`) | ✓ canonical code, native name still resolves |

So the **Set Code is irrelevant** — the Set Name is the lever. **The prior `GK2_AZORIU`-in-Set-Code approach operated on the column DragonShield discards; it never worked end-to-end** (the unit tests only checked our own model→CSV→model round-trip, not DragonShield's resolution). The correct fix is a Set-Name alias (`gk{n} #N → "Guild Kit: <Guild>"`), the same write-side pattern `DeckboxMap` uses for editions. The earlier "native codes work" round-trip is explained by those cards being exported *from* DragonShield with both the native code *and* the native set name — it was the name doing the work.

---

## TopDecked

Status: 43/43 rows round-trip cleanly (May 2026). Full parity with Moxfield. **June 2026:** the 29-row reference set round-trips cleanly — all 11 languages preserved (incl. Spanish, where Archidekt's Moxfield path dropped it), etched preserved, borderless → exact Scryfall ID, guild kit `gk2 #1`, The List `plst`. No finish auto-correction needed (we sent the right finishes). Gold-standard result alongside Moxfield/Manabox.

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
- **Collapses etched → foil** on storage, and its native importer **rejects the literal `etched`** value in the Foil column (accepts only `foil`/blank). June 2026: our `DECKBOX` writer regressed here — the tri-state `CardFinish` migration left `appsettings` `Etched: "etched"`, so we now emit `etched` and Deckbox drops the row (Demonic Tutor CMM #509 was the one rejected row of 29). Fix: `Etched: null`, so the writer falls back to the `foil` string (the DragonShield pattern).
- **Strips letter prefixes / reshapes collector numbers**: `puma U8` → `puma 8`. June 2026 confirmed the same on special products — The List `DDC-49` → number `49` + Printing Note `DDC`; Secret Lair `VS` → `801`. These resolved correctly *on Deckbox* (its Scryfall ID matched), but are lossy for re-import via `(set, #)` — `plst/49` and `sld/801` aren't the real Scryfall coordinates. Resolving by the Scryfall ID Deckbox provides would recover them.
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
- **Cross-importing our *other-format* files (June 2026):** our `manabox.csv` lost the 5 special products whose set names don't match Deckbox editions (Clue, Food, Viscera Seer, Isperia, Ral's Vanguard). Deckbox's Manabox-importer resolves by **Scryfall ID** when present and falls back to name+set otherwise — but our writer emits no Scryfall ID, so those rows can't resolve. Our `moxfield.csv` was rejected wholesale (`The header of the csv file is not correct`) — our 8-column output isn't Moxfield's real export header. Both point at the same gap: we emit simplified subsets without the Scryfall ID, so third-party "X-format" importers can't resolve the hard cases.

**Writer-modeling implications** for `MtgCsvHelper`'s `DECKBOX` writer (follow-up issue):
1. Emit lowercase set codes (already mostly handled — depends on source).
2. Emit `foil` for etched cards — **currently broken**: `appsettings` `DECKBOX.Etched` is `"etched"`, which the native importer rejects. Set it to `null` so the writer emits `foil`.
3. **Need Set Name aliasing** for divergent sets (Secret Lair, Box Toppers, token sets) — leaving Edition blank only auto-resolves for some codes; missing the alias leaves cards in "Unspecified Edition". Small hand-curated mapping table.
4. Letter-prefix collector numbers (`U8`) pass through cleanly; Deckbox normalizes server-side. No need to strip in our writer.

---

## TCGPlayer

Status: 43 rows, **synthetic only** (not site-blessed). TCGPlayer's CSV import/export is paywalled — restricted to **Level 4 Seller** accounts. Round-trip blessing isn't feasible without paid seller access.

**Format reference**: real TCGPlayer export available at `Resources/SampleCsvs/Collection/tcgplayer-collection.csv` (1000+ rows from a real seller account, different cards than our 43-row test set).

**Schema** (from the real export):
- 12 columns: `Quantity,Name,Simple Name,Set,Card Number,Set Code,Printing,Condition,Language,Rarity,Product ID,SKU`.
- `Name` vs `Simple Name` (verified against real exports, May 2026): `Simple Name` strips **parenthetical variant tags** only (`(Borderless)`, `(Showcase)`, `(Retro Frame)`, `(Extended Art)`, `(0269)`, `(No PW Symbol)`). Commas, apostrophes, and most punctuation are **preserved in both columns**. Example: `Aragorn, Hornburg Hero (Borderless)` in `Name` becomes `Aragorn, Hornburg Hero` in `Simple Name`; `"Elesh Norn, Grand Cenobite"` is identical in both. Diacritics ARE normalized in both columns (`Lim-Dûl` → `Lim-Dul`) — this is a real catalog mismatch handled by NFD-normalization in `NamesMatch`. Our `appsettings.json` reads `Simple Name` because the absence of `(Borderless)`/`(Showcase)` suffixes matches Scryfall's canonical names directly.
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

**Condition vocabulary**: TCGPlayer-standard 5 strings (`NM, LP, MP, HP, D`) — no Mint, no Excellent. Our 7-enum model collapses both `Mint` and `Excellent` to `"NM"` on writes via the null-alias mechanism: `appsettings.json` declares Archidekt's `Mint` and `Excellent` as `null`, and `CardConditionConverter.ConvertToString` falls back to the NearMint string for both. The read path doesn't match a null config entry, so `"NM"` cleanly resolves to `NEAR_MINT` regardless of switch arm order. Covered by `CardConditionConverterTests`.

**Archidekt's moxfield-format importer is lossy on conditions** (mirroring DragonShield's pattern). Importing our 6 distinct moxfield condition strings, Archidekt re-emits only 4:

| Moxfield string we wrote | Archidekt re-export |
|---|---|
| `Mint` | `NM` (Archidekt has no Mint) |
| `Near Mint` | `NM` ✓ |
| `Good (Lightly Played)` | **`NM`** (cross-format importer doesn't recognize the parenthetical) |
| `Played` (Moxfield's LightlyPlayed equivalent) | `MP` ✓ |
| `Heavily Played` (Moxfield's Played equivalent) | `HP` ✓ |
| `Damaged` | `D` ✓ |

The LP collapse is **specific to Archidekt's moxfield-importer**, not their condition vocabulary — their UI dropdown has `LP` as a valid option, but the moxfield cross-import doesn't map our literal `"Good (Lightly Played)"` string to it.

**Language vocabulary** uses Archidekt-specific 2-letter codes that diverge from common conventions for some entries:
- `EN, DE, FR, IT, ES, PT, RU` (standard ISO)
- `JP` not `JA` (Japanese)
- `KR` not `KO` (Korean)
- `CS` for Chinese Simplified (not `zhs` or `zh-CN`)
- `CT` for Chinese Traditional (not `zht` or `zh-TW`)

**No native finish or set-name aliasing quirks** observed — Archidekt accepted everything from the 42-row Manabox source plus normalized its own additions cleanly. Cross-format diversity (Demonic Tutor across 8 sets, variant-finish cards, accent + apostrophe names) all imported without errors or normalization.

**Live round-trip (June 2026):**
- **No native Archidekt-format import.** Archidekt's importer offers Cardsphere / Deckbox / Delver Lens / Dragonshield / Helvault / ManaBox / Moxfield — but **not Archidekt**. So our `archidekt.csv` is a worked example of Archidekt's *export* shape and a fixture for our *reader*, but can't be round-tripped back into Archidekt; verification must go in via a sibling format.
- **Cross-importers are positional, no drag-to-rearrange.** Both the Moxfield and ManaBox importers expect the source tool's *exact* export column order. Our writers emit canonical subsets (Moxfield 8 columns vs Moxfield's real 13; Manabox 9 vs ManaBox's 17) in a different order, so every source format needs manual per-column reassignment.
- **Cards resolve correctly once mapped.** Via the Moxfield path: borderless Orcish Bowmasters → `ltr 433` with the exact borderless Scryfall ID `de2de055…`; guild kit `gk2 #1` (resolves by collector number, unlike DragonShield); The List `plst`; etched preserved; adventure/split full names.
- **Spanish → EN** on the Moxfield path: of 11 languages, only Spanish dropped to English (the rest mapped). Same lossy-cross-importer class as the condition collapses above — not our bug (Moxfield itself preserved Spanish). The Manabox path preserves it; prefer that for fidelity (though it needs the same manual column mapping).

---

## Cardmarket (read-only, by `idProduct`)

Status: 40 rows (May 2026). Generated from `moxfield-reference-collection.csv` via `tools/MtgCsvHelper.RefreshReferenceData -- cardmarket-fixture`. The generator parses the moxfield reference, looks up each printing's `cardmarket_id` in the catalog, and emits a Cardmarket-shaped row. 3 of the 43 moxfield rows resolved to catalog entries that have `cardmarket_id = null`: Clue token (tmh2 #14), Treasure token (tclb #17), Demonic Tutor 30th Anniversary (30a #101). Cardmarket *does* list these (e.g. [Clue Token V1 / MH2 Extras](https://www.cardmarket.com/en/Magic/Products/Singles/Modern-Horizons-2-Extras/Clue-Token-V1)); the gap is in Scryfall's data — its `cardmarket_id` field isn't populated for these printings. Two of the three (`tclb #17`, `30a #101`) even have `tcgplayer_id` populated, so it's specifically a Scryfall ↔ Cardmarket crosslink gap. Skipping is the right call here — closing the gap requires either Scryfall data updates or a Cardmarket product-search fallback (out of scope).

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
