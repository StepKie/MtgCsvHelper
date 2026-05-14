# Fault-Tolerance Sample CSVs

Hand-crafted Moxfield-format CSVs that exercise each branch of the new fault-tolerant import path. Use them to eyeball Console + Blazor behavior end-to-end before merging #40.

All files are intended to be loaded with **input format `MOXFIELD`** (except #6, which deliberately mismatches).

## Files

| File | What it tests | Expected result |
|---|---|---|
| `01-happy-path.csv` | All valid rows, real Scryfall set codes | 3 cards, 0 errors, 0 warnings |
| `02-mixed-valid-invalid.csv` | Valid rows interspersed with rows whose `Count` cell is non-numeric | 2 cards, 2 errors |
| `03-all-invalid.csv` | Every row has a non-numeric `Count` | 0 cards, 3 errors |
| `04-warnings-only.csv` | Valid rows but with set codes not in Scryfall (`FAKESET1` etc.) | 3 cards, 0 errors, 3 warnings |
| `05-mixed-warnings-errors.csv` | One valid + one warning + one error + one warning | 3 cards, 1 error, 2 warnings (invariant: `cards + errors == 4`) |
| `06-wrong-format-dragonshield-as-moxfield.csv` | A DragonShield-shaped CSV — when imported as MOXFIELD, the required `Count`/`Name` headers are missing | `HeaderValidationException` thrown — Blazor surfaces a red header-error alert; Console aborts |
| `07-blank-and-delimiter-only-rows.csv` | Two valid rows interleaved with a truly blank line and a `,,,,,,,` line | 2 cards, 0 errors, 0 warnings |

## Running locally

### Blazor (recommended for visual verification)

```bash
dotnet run --project MtgCsvHelper.BlazorWebAssembly
```

Open the printed URL, pick `MOXFIELD` as input format, upload one of the CSVs, click **Convert**, observe:
- the issues list (severity-coloured)
- the **Download Error Report .csv** button (only when errors > 0)
- the red header-error alert (only for file #6)

### Console

```bash
# from a directory containing the sample CSV(s):
dotnet run --project /path/to/MtgCsvHelper.Console -- --in MOXFIELD --out MANABOX -f 02-mixed-valid-invalid.csv
```

The console logs `{file}: N cards parsed, M errors, K warnings` followed by one line per issue.
