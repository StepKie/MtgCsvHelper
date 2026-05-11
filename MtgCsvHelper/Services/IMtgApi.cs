namespace MtgCsvHelper.Services;

/// <summary>
/// Network access to Scryfall for printings NOT covered by the locally-bundled
/// <see cref="IReferenceCardCatalog"/>. Today: lookups by cardmarket_id for cards
/// added since the bundle was generated. Returns the canonical <see cref="ReferenceCard"/>
/// shape so callers never see the underlying Scryfall library types.
/// </summary>
public interface IMtgApi
{
	// Batched lookup of cards by cardmarket_id. Missing IDs are absent from the returned dictionary.
	// Cached: subsequent calls for already-resolved IDs return instantly without hitting the network.
	// The token aborts the in-flight Scryfall request and any inter-request delay.
	Task<IReadOnlyDictionary<int, ReferenceCard>> GetCardsByCardmarketIdsAsync(IEnumerable<int> cardmarketIds, CancellationToken ct = default);
}
