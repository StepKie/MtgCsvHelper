namespace MtgCsvHelper.Enrichment;

/// <summary>
/// One parsed CSV row threaded through the enrichment pipeline. Bundles the card with its
/// source row number — and the raw CSV line it came from, so post-parse enrichers can include
/// the raw text on any issues they emit (the UI surfaces it next to the row number for
/// quick diagnosis). Note: the wrapper is a record (positional + value-equality) but the
/// inner <see cref="PhysicalMtgCard.Printing"/> is mutated in place by enrichers — the
/// record buys no immutability for the contained data, only a clean (card, row) pair shape.
/// </summary>
public sealed record ParsedRow(PhysicalMtgCard Card, int RowNumber, string? RawContent = null);
