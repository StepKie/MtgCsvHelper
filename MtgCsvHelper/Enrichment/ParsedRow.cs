namespace MtgCsvHelper.Enrichment;

/// <summary>
/// One parsed CSV row threaded through the enrichment pipeline. Bundles the card with its
/// source row number so error reporting stays accurate even after intermediate enrichers
/// drop or reorder rows.
/// </summary>
public sealed record ParsedRow(PhysicalMtgCard Card, int RowNumber);
