# Reference collection

The canonical, lossless reference collection: one card (or a minimal group) per dimension the
converter must round-trip. The source of truth is
[`master.csv`](../../../../MtgCsvHelper.Tests/Resources/master.csv) — written in the internal
`CANONICAL` format (one column per model field). **The per-format CSVs in this folder are
generated from it** by `ReferenceCsvSyncTests`; do not hand-edit them. To change the set, edit
`master.csv` and re-run that test — it regenerates every file here and fails on drift.

These files double as (a) worked examples of a valid export for each format and (b) the artifacts
fed to each live site for the import check below.

## What the rows demonstrate

| Rows | Card(s) | Dimension exercised |
|---|---|---|
| DFC baseline + foreign language | Ambitious Farmhand // Seasoned Cathar (MID #2), `en` and `zht` | Double-faced card name (front+back), and a non-English language tag |
| Count + foil + price | same DFC, qty 2, `de`, foil, price | Multi-copy, foil finish, a third language, purchase price |
| Condition grades | same DFC at Good / Lightly Played / Played / Poor (Mint + Near Mint covered above) | Condition vocabulary. *Excellent is omitted — not all sites have it.* |
| Tokens | Clue (TMH2 #14), Food (TLTR #10) | Token-set name / collector-number encoding, token prices |
| Etched | Demonic Tutor (CMM #509) | Etched finish; degrades to foil on sites with no etched tier |
| Language sweep | Lightning Bolt (M11 #149) × all 11 mapped languages | Each site's distinct language vocabulary (e.g. Archidekt `JP`/`KR`/`CS`/`CT`, Manabox `zh_CN`/`zh_TW`, Cardmarket numeric ids) |
| Special products | Mardu Outrider (MB2 #1), Viscera Seer (SLD #VS), Isperia, Supreme Judge (GK2 #1), Ral's Vanguard (CMB1 #1), Demonic Tutor (PLST #DDC-49) | Non-standard set codes and collector-number shapes: Mystery Booster 2, Secret Lair, Ravnica Guild Kit, playtest cards, and The List's `<origset>-<num>` numbers. Acceptance per site tracked below. |
| Borderless / alternate frame | Orcish Bowmasters (LTR #433 — borderless; #103 is the normal printing) | Variant-frame printing: TCGplayer decorates the name (`Orcish Bowmasters (Borderless)`) while keeping `Simple Name` plain; other sites encode the variant in the collector number. CardKingdom omits the collector number (`title,edition,foil,quantity`), so its row can't distinguish the borderless from the normal printing — no borderless signal there. Catches the alternate-printing mismatch from the borderless issue. |
| Non-transform DFC layouts | Brazen Borrower // Petty Theft (ELD #39 — adventure), Fire // Ice (APC #128 — split) | DragonShield writes the full `A // B` name for adventure/split, unlike the short name for the transform DFC in row 1. Verifies layout-specific name handling — these rows already surfaced that our writer shortens adventure (`Brazen Borrower`) but keeps split full, which the live import will adjudicate. |

Deliberately **not** yet covered (lossless-only, added as the underlying support lands):
non-English card *names* and regional set codes, and foil *treatments* like Rainbow Foil.

## Manual import checklist

Automated tests prove our `model → CSV → model` round-trip; they cannot prove a real site *accepts*
the CSV we emit. That's this checklist: import each generated file into its live site, confirm no
rows are rejected, then re-export and confirm the cards survive. File an issue with the
[`site-import-error`](https://github.com/StepKie/MtgCsvHelper/labels/site-import-error) label for any
failure.

| Format | File | Import at | Imports cleanly (no rejected rows)? | Re-export carries the same cards? |
|---|---|---|---|---|
| Moxfield | `moxfield.csv` | moxfield.com → Collection → Import | ☐ | ☐ |
| DragonShield | `dragonshield.csv` | mtg.dragonshield.com → Import | ☐ | ☐ |
| Manabox | `manabox.csv` | Manabox app → Import CSV | ☐ | ☐ |
| TopDecked | `topdecked.csv` | TopDecked app → Import | ☐ | ☐ |
| Deckbox | `deckbox.csv` | deckbox.org → Mtg → Import | ☐ | ☐ |
| Archidekt | `moxfield.csv` / `manabox.csv` | archidekt.com → Collection → Import (Archidekt has **no native-format import**; use a sibling format, and expect to remap columns manually — its importers are positional) | ☐ | ☐ |
| MTGGoldfish | `mtggoldfish.csv` | mtggoldfish.com → Collection → Import | ☐ | ☐ |
| TCGplayer | `tcgplayer.csv` | tcgplayer.com → mass entry / app | ☐ | ☐ |
| MTGO | `mtgo.csv` | MTGO client → import | ☐ | n/a (client, no CSV re-export) |
| CardKingdom | `cardkingdom.csv` | cardkingdom.com → sell/buylist entry | ☐ | n/a (write-only buylist) |

Cardmarket is import-only for us (we identify its cards by `idProduct` via Scryfall reverse lookup),
so there is no generated Cardmarket file to import.

## Special-product coordinate acceptance

The reference set carries five non-standard coordinates (the "Special products" row above).
Automated round-trip only proves we read back our own output; it cannot prove a live site resolves
these to the *correct* printing. Live results from the June 2026 import sweep are below.

What our writer emits per coordinate:

| Coordinate | Reference card | Emitted as |
|---|---|---|
| The List | Demonic Tutor PLST #DDC-49 | `plst` / `DDC-49` (canonical) |
| Ravnica Guild Kit | Isperia, Supreme Judge GK2 #1 | canonical `gk2` / `RNA Guild Kit` (the `GK2_AZORIU` code is ignored by DragonShield — it needs the native *set name*; fix pending) |
| Secret Lair | Viscera Seer SLD #VS | `sld` / `VS` |
| Mystery Booster 2 | Mardu Outrider MB2 #1 | `mb2` / `1` |
| Playtest cards | Ral's Vanguard CMB1 #1 | `cmb1` / `1` |

Live results (June 2026 sweep — "✅ all five" = all resolve to the correct printing):

| Format | Result |
|---|---|
| Moxfield | ✅ all five |
| TopDecked | ✅ all five |
| Manabox | ✅ all five (native import) |
| Archidekt | ✅ all five (via Moxfield import; needs manual column mapping) |
| Deckbox | ✅ all five resolve, but The List `DDC-49`→`49` and Secret Lair `VS`→`801` collector numbers are reshaped — lossy for re-import via `(set,#)` |
| DragonShield | ❌ guild kit → Return to Ravnica (wrong); The List → Duel Decks (demangled). MB2 / SLD / CMB1 ✅ |
| MTGGoldfish | ☐ not tested (Premium-gated) |
| TCGplayer | ☐ not tested (Level-4-seller-gated) |
| MTGO | ☐ not tested (import-only, no CSV re-export) |
| CardKingdom | n/a (write-only; no collector-number column, can't encode most variants) |

Key findings (full detail in [`../Tests/SITE_BEHAVIOR.md`](../Tests/SITE_BEHAVIOR.md)):

- **Scryfall-aligned sites** (Moxfield, TopDecked, Manabox, Archidekt, Deckbox) resolve the guild kit and The List by collector number — no special handling needed. The guild-kit mis-resolution is **DragonShield-only**.
- **DragonShield resolves by Set *Name*, not Set Code.** The guild kit fails because our `RNA Guild Kit` ≠ DragonShield's `Guild Kit: Azorius`; the `GK2_AZORIU` code is ignored. Fix: emit the native per-guild set name. The List demangles to the original printing — unfixable (no The List concept).
- **Deckbox rejects the literal `etched`** finish on native import (drops that row); fix: emit `foil`. It also reshapes special-product collector numbers — resolving by the Scryfall ID Deckbox provides would recover them on re-import.
- **Archidekt has no native-format import**, and its cross-importers are positional, so any source format needs manual column mapping; the Moxfield path also drops `Spanish→EN`.
