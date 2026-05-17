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

/// <summary>
/// Convenience base for the common case: a sync, per-card step that may drop the row.
/// Iterates in reverse so removals don't shift downstream indices.
/// </summary>
public abstract class PerCardEnricher : IEnricher
{
	public Task EnrichAsync(IList<ParsedRow> rows, ICollection<ImportIssue> issues, CancellationToken ct)
	{
		for (int i = rows.Count - 1; i >= 0; i--)
		{
			ct.ThrowIfCancellationRequested();
			if (!EnrichOne(rows[i], issues))
			{
				rows.RemoveAt(i);
			}
		}
		return Task.CompletedTask;
	}

	/// <returns><c>true</c> to keep the row, <c>false</c> to drop it.</returns>
	protected abstract bool EnrichOne(ParsedRow row, ICollection<ImportIssue> issues);
}
