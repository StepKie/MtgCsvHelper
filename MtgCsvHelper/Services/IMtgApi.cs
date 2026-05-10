using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

/// <summary>
/// Network access to Scryfall for data NOT covered by the locally-bundled
/// <see cref="IReferenceCardCatalog"/>. Today: lookups by cardmarket_id for
/// printings the user has imported but that aren't in the bundle (e.g. cards
/// added since the bundle was generated). PR 3 (#48 part 3) will fold this
/// into the catalog as a fallback layer and retire this interface.
/// </summary>
public interface IMtgApi
{
	// Batched lookup of Scryfall cards by their cardmarket_id. Missing IDs are absent from the returned dictionary.
	// Cached: subsequent calls for already-resolved IDs return instantly without hitting the network.
	Task<IReadOnlyDictionary<int, Card>> GetCardsByCardmarketIdsAsync(IEnumerable<int> cardmarketIds);
}
