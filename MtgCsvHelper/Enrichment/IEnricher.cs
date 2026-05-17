namespace MtgCsvHelper.Enrichment;

/// <summary>
/// One step of the post-parse processing pipeline. Each step receives the current row list,
/// can mutate cards in place, can drop rows, and can append issues. Steps run in registration
/// order — order is significant (e.g. catalog validation runs before Cardmarket resolution so
/// Scryfall-resolved cards bypass the validator and avoid being dropped against a stale bundle).
/// Named "Enricher" after the dominant role (4 of 5 known implementations transform card data);
/// the one validator class is named <c>CatalogValidator</c> rather than pretending to be an enricher.
/// </summary>
public interface IEnricher
{
	/// <param name="rows">Mutable list of parsed rows. Implementations may modify cards in place
	/// and call <c>RemoveAt</c> to drop invalid/unresolvable rows — passing a fixed-size or
	/// read-only <see cref="IList{T}"/> (e.g. arrays, <c>ReadOnlyCollection</c>) will throw.</param>
	Task EnrichAsync(IList<ParsedRow> rows, ICollection<ImportIssue> issues, CancellationToken ct);
}
