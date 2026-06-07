namespace MtgCsvHelper.Enrichment;

/// <summary>
/// Convenience base for the common case: a sync, per-card step that may drop the row.
/// Iterates in reverse so removals don't shift downstream indices — as a side effect,
/// issues appended during a single pass land in reverse row order. <c>ParseCollectionCsvAsync</c>
/// sorts the final issue list by <see cref="ImportIssue.RowNumber"/> before returning.
/// </summary>
public abstract class PerCardEnricher : IEnricher
{
	public Task EnrichAsync(List<ParsedRow> rows, ICollection<ImportIssue> issues, CancellationToken ct)
	{
		for (int i = rows.Count - 1; i >= 0; i--)
		{
			ct.ThrowIfCancellationRequested();
			var row = rows[i];
			if (EnrichOne(ref row, issues))
			{
				rows[i] = row;
			}
			else
			{
				rows.RemoveAt(i);
			}
		}
		return Task.CompletedTask;
	}

	/// <summary> The row is passed by reference so implementations can replace it (records are immutable). </summary>
	/// <returns><c>true</c> to keep the row, <c>false</c> to drop it.</returns>
	protected abstract bool EnrichOne(ref ParsedRow row, ICollection<ImportIssue> issues);
}
