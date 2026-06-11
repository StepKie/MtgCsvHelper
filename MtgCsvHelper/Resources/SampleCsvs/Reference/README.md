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
| Archidekt | `archidekt.csv` | archidekt.com → Collection → Import | ☐ | ☐ |
| MTGGoldfish | `mtggoldfish.csv` | mtggoldfish.com → Collection → Import | ☐ | ☐ |
| TCGplayer | `tcgplayer.csv` | tcgplayer.com → mass entry / app | ☐ | ☐ |
| MTGO | `mtgo.csv` | MTGO client → import | ☐ | n/a (client, no CSV re-export) |
| CardKingdom | `cardkingdom.csv` | cardkingdom.com → sell/buylist entry | ☐ | n/a (write-only buylist) |

Cardmarket is import-only for us (we identify its cards by `idProduct` via Scryfall reverse lookup),
so there is no generated Cardmarket file to import.

## Special-product coordinate acceptance

The reference set carries five non-standard coordinates (the "Special products" row above).
Automated round-trip only proves we read back our own output; it cannot prove a live site resolves
these to the *correct* printing. The hazard is silent mis-resolution — a site that name-only matches
picks the wrong printing with no error shown. Import each generated file, then confirm the
special-product rows land on the right card.

What our writer emits per coordinate:

| Coordinate | Reference card | Emitted as |
|---|---|---|
| The List | Demonic Tutor PLST #DDC-49 | `plst` / `DDC-49` (canonical) |
| Ravnica Guild Kit | Isperia, Supreme Judge GK2 #1 | `GK2_AZORIU` to DragonShield; canonical `gk2` everywhere else |
| Secret Lair | Viscera Seer SLD #VS | `sld` / `VS` |
| Mystery Booster 2 | Mardu Outrider MB2 #1 | `mb2` / `1` |
| Playtest cards | Ral's Vanguard CMB1 #1 | `cmb1` / `1` |

| Format | All five special-product rows resolve to the correct printing? |
|---|---|
| Moxfield | ☐ |
| DragonShield | ☐ |
| Manabox | ☐ |
| TopDecked | ☐ |
| Deckbox | ☐ |
| Archidekt | ☐ |
| MTGGoldfish | ☐ |
| TCGplayer | ☐ |
| MTGO | ☐ |
| CardKingdom | n/a (write-only buylist) |

Known results so far (from prior round-trips logged in [`../Tests/SITE_BEHAVIOR.md`](../Tests/SITE_BEHAVIOR.md)):

- **DragonShield honors MB2 / SLD / CMB1 under canonical codes** (June 2026 full canonical round-trip): Mystery Booster 2, Secret Lair, and playtest cards all re-exported on the correct printing. Guild kits are the *only* special product DragonShield mis-resolves under canonical codes — so no per-product native-code table is needed beyond guild kits.
- **DragonShield demangles The List**: `plst` rows re-export as the original printing (Demonic Tutor PLST DDC-49 → DVD #49). The card survives but the coordinate normalizes away from `plst` and can't round-trip back — unfixable, DragonShield has no The List concept.
- **DragonShield guild kits**: the canonical `gk2` name-matches onto the wrong edition (Isperia GK2 #1 → Return to Ravnica #171); the native `GK2_AZORIU` we now emit resolves correctly.
- **Moxfield / Manabox / TopDecked / Archidekt** carry `plst` + `<origset>-<num>` natively in real exports, so The List resolves there. Secret Lair / MB2 / playtest acceptance on those sites is unverified.
