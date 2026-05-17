namespace MtgCsvHelper.Enrichment;

/// <summary>
/// One parsed CSV row threaded through the enrichment pipeline. Bundles the card with its
/// source row number so error reporting stays accurate even after intermediate enrichers
/// drop or reorder rows. Note: the wrapper is a record (positional + value-equality) but
/// the inner <see cref="PhysicalMtgCard.Printing"/> is mutated in place by enrichers —
/// the record buys no immutability for the contained data, only a clean (card, row) pair shape.
/// </summary>
public sealed record ParsedRow(PhysicalMtgCard Card, int RowNumber);
