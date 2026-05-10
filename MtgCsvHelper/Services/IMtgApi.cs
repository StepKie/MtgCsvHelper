using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

// TODO: retire once the catalog handles cardmarket_id fallbacks (issue #48).
/// <summary>
/// Network access to Scryfall for data NOT covered by the locally-bundled
/// <see cref="IReferenceCardCatalog"/>. Today: lookups by cardmarket_id for
/// printings the user has imported but that aren't in the bundle (e.g. cards
/// added since the bundle was generated).
/// </summary>
public interface IMtgApi
{
	// Batched lookup of Scryfall cards by their cardmarket_id. Missing IDs are absent from the returned dictionary.
	// Cached: subsequent calls for already-resolved IDs return instantly without hitting the network.
	Task<IReadOnlyDictionary<int, Card>> GetCardsByCardmarketIdsAsync(IEnumerable<int> cardmarketIds);
}
