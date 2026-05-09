using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

public interface IMtgApi
{
	IEnumerable<Set> GetSets();
	Task<IEnumerable<Set>> GetSetsAsync();

	List<string> GetDoubleFacedCardNames();
	Task<List<string>> GetDoubleFacedCardNamesAsync();

	Task<IEnumerable<Card>> GetTokenCardNamesAsync();
	List<string> GetTokenCardNames();

	// Batched lookup of Scryfall cards by their cardmarket_id. Missing IDs are absent from the returned dictionary.
	// Cached: subsequent calls for already-resolved IDs return instantly without hitting the network.
	Task<IReadOnlyDictionary<int, Card>> GetCardsByCardmarketIdsAsync(IEnumerable<int> cardmarketIds);

	Task LoadData();
}
