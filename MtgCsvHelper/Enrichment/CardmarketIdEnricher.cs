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
	public async Task EnrichAsync(List<ParsedRow> rows, ICollection<ImportIssue> issues, CancellationToken ct)
	{
		// CardMarketId is `int` (not `int?`) in the Scryfall library, so 0 acts as the "unset" sentinel —
		// real cardmarket_ids are always positive in Scryfall data.
		var pending = new List<(int Index, int Id)>();
		for (int i = 0; i < rows.Count; i++)
		{
			var c = rows[i].Card;
			if (c.Printing.CardMarketId > 0 && string.IsNullOrEmpty(c.Printing.Name))
			{
				pending.Add((i, c.Printing.CardMarketId));
			}
		}

		if (pending.Count == 0) { return; }

		var ids = pending.Select(p => p.Id).Distinct().ToList();
		var resolved = await resolver.ResolveAsync(ids, ct);

		// Walk in reverse so RemoveAt doesn't shift positions we still need to visit. Invariant:
		// pending was built by walking rows forward, so its Index values are strictly ascending —
		// reverse iteration here processes rows high-to-low, and each RemoveAt(i) only shifts
		// positions we've already visited. Duplicate CardMarketIds across rows are handled
		// correctly: each row has its own entry in `pending` and both look up the same `full`
		// record below (TryGetValue is idempotent).
		for (int k = pending.Count - 1; k >= 0; k--)
		{
			var (i, id) = pending[k];
			var row = rows[i];
			if (resolved.TryGetValue(id, out var full))
			{
				row.Card.Printing.Name = full.Name;
				row.Card.Printing.Set = full.Set;
				row.Card.Printing.SetName = full.SetName;
				row.Card.Printing.CollectorNumber = full.CollectorNumber;
				rows[i] = row with { Card = row.Card with { Rarity = full.Rarity } };
			}
			else
			{
				// Without a name/set, we can't write a meaningful row — drop the card.
				// This is data loss (Error), not just degraded fidelity (Warning).
				// One error per affected row (not per distinct ID), so duplicate-ID inputs that
				// fail to resolve produce one issue per row — keeps row-level traceability.
				issues.Add(new ImportIssue(IssueSeverity.Error, row.RowNumber,
					$"Cardmarket ID {id} not found in Scryfall data — card skipped",
					RawContent: row.RawContent));
				rows.RemoveAt(i);
			}
		}
	}
}
