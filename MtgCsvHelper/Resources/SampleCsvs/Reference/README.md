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

Deliberately **not** yet covered (lossless-only, added as the underlying support lands):
promo / Secret Lair / non-standard set names (needs the `all_cards` bundle, #92), non-English card
*names* and regional set codes (#103), and foil *treatments* like Rainbow Foil (#115).

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
