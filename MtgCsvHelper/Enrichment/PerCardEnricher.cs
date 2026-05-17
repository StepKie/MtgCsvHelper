namespace MtgCsvHelper.Enrichment;

/// <summary>
/// Convenience base for the common case: a sync, per-card step that may drop the row.
/// Iterates in reverse so removals don't shift downstream indices — as a side effect,
/// issues appended during a single pass land in reverse row order. Consumers that care
/// about ordering should sort by <see cref="ImportIssue.RowNumber"/>.
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
