namespace MtgCsvHelper.Services;

/// <summary>
/// Default <see cref="ICardmarketResolver"/>: catalog first, batched Scryfall fallback for misses.
/// No additional cache layer — <see cref="IMtgApi"/> already caches its network results in-process,
/// and catalog lookups are O(1) dictionary reads, so stacking another dictionary buys nothing.
/// </summary>
public sealed class CardmarketResolver(IReferenceCardCatalog catalog, IMtgApi api) : ICardmarketResolver
{
	public async Task<IReadOnlyDictionary<int, ReferenceCard>> ResolveAsync(IEnumerable<int> cardmarketIds, CancellationToken ct = default)
	{
		var resolved = new Dictionary<int, ReferenceCard>();
		var misses = new List<int>();

		// No Distinct here: handler dedupes before calling; CachedMtgApi dedupes again at the network boundary.
		foreach (var id in cardmarketIds)
		{
			var hit = catalog.FindByCardmarketId(id);
			if (hit is not null) { resolved[id] = hit; }
			else { misses.Add(id); }
		}

		if (misses.Count > 0)
		{
			var fetched = await api.GetCardsByCardmarketIdsAsync(misses, ct);
			foreach (var (id, card) in fetched)
			{
				resolved[id] = card;
			}
		}

		return resolved;
	}
}
