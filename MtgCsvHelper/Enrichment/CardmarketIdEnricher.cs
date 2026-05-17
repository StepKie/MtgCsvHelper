using MtgCsvHelper.Services;

namespace MtgCsvHelper.Enrichment;

/// <summary>
/// Resolves Cardmarket stub rows — those parsed with only <c>Printing.CardMarketId</c> set —
/// to full Scryfall cards via batched <c>/cards/collection</c> lookup. Rows whose ID doesn't
/// resolve are dropped with an Error. Other formats are passed through unchanged (their
/// rows have no <c>CardMarketId</c>). Implements <see cref="IEnricher"/> directly rather
/// than via <see cref="PerCardEnricher"/> because the batched network call is the whole point.
/// </summary>
public sealed class CardmarketIdEnricher(ICardmarketResolver resolver) : IEnricher
{
	public async Task EnrichAsync(IList<ParsedRow> rows, ICollection<ImportIssue> issues, CancellationToken ct)
	{
		// CardMarketId is `int` (not `int?`) in the Scryfall library, so 0 acts as the "unset" sentinel —
		// real cardmarket_ids are always positive in Scryfall data.
		var pendingIndices = new List<int>();
		for (int i = 0; i < rows.Count; i++)
		{
			var c = rows[i].Card;
			if (c.Printing.CardMarketId > 0 && string.IsNullOrEmpty(c.Printing.Name))
			{
				pendingIndices.Add(i);
			}
		}

		if (pendingIndices.Count == 0) { return; }

		var ids = pendingIndices.Select(i => rows[i].Card.Printing.CardMarketId).Distinct().ToList();
		var resolved = await resolver.ResolveAsync(ids, ct);

		// Walk in reverse so removals don't shift indices we still need.
		for (int k = pendingIndices.Count - 1; k >= 0; k--)
		{
			var i = pendingIndices[k];
			var row = rows[i];
			var id = row.Card.Printing.CardMarketId;
			if (resolved.TryGetValue(id, out var full))
			{
				row.Card.Printing.Name = full.Name;
				row.Card.Printing.Set = full.Set;
				row.Card.Printing.SetName = full.SetName;
				row.Card.Printing.CollectorNumber = full.CollectorNumber;
			}
			else
			{
				// Without a name/set, we can't write a meaningful row — drop the card.
				// This is data loss (Error), not just degraded fidelity (Warning).
				issues.Add(new ImportIssue(IssueSeverity.Error, row.RowNumber, $"Cardmarket ID {id} not found in Scryfall data — card skipped"));
				rows.RemoveAt(i);
			}
		}
	}
}
