namespace MtgCsvHelper.Services;

/// <summary>
/// Resolves Cardmarket product IDs to <see cref="ReferenceCard"/> printings. Catalog-first:
/// the bundled <see cref="IReferenceCardCatalog"/> is consulted in-process before any network
/// call. Misses fall back to <see cref="IMtgApi"/> in a single batched request.
/// </summary>
public interface ICardmarketResolver
{
	/// <summary>
	/// Resolve a batch of cardmarket_ids. IDs absent from both the catalog and Scryfall are
	/// absent from the returned dictionary (caller decides how to surface the miss).
	/// </summary>
	Task<IReadOnlyDictionary<int, ReferenceCard>> ResolveAsync(IEnumerable<int> cardmarketIds);
}
