using System.Text.Json;
using ScryfallApi.Client;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

/// <summary>
/// On creation, loads queried data via Task.Run into static properties.
/// These can only be successfully queried after a short delay, if necessary, await LoadData()
/// </summary>
public class CachedMtgApi : IMtgApi
{
	readonly IScryfallApiClient _api;
	List<string> _doubleFacedCardNames;
	List<string> _tokenCardNames;
	List<Set> _sets;

	public CachedMtgApi(ScryfallApiClient api)
	{
		_api = api;
		Console.WriteLine("CachedMtgApi created");
		Log.Debug("CachedMtgApi created");
	}

	public async Task LoadData()
	{
		_sets ??= (await GetSetsAsync()).ToList();
		_doubleFacedCardNames ??= await GetDoubleFacedCardNamesAsync();
		_tokenCardNames ??= (await GetTokenCardNamesAsync()).Select(c => c.Name).Distinct().ToList();
	}

	public async Task<List<string>> GetDoubleFacedCardNamesAsync()
	{
		var cards = await _api.Catalogs.ListCardNames();
		var names = cards.Where(name => name.Contains(" // ")).ToList() ?? [];

		Console.WriteLine($"Loaded {names.Count} double-faced cards from server");
		Log.Debug($"Loaded {names.Count} double-faced cards from server");

		return names;
	}

	public async Task<IEnumerable<Card>> GetTokenCardNamesAsync()
	{
		// Unfortunately, the IScryfallApiClient has an issue with encoding the query string, so we use our own HttpClient
		var httpClient = new HttpClient();
		Uri query = new("https://api.scryfall.com/cards/search?q=set_type=Token&include_extras=true");
		var hasMore = true;
		List<Card> allCards = [];

		while (hasMore)
		{
			try
			{
				var response = await httpClient.GetAsync(query);
				var content = await response.Content.ReadAsStringAsync();
				var cards = JsonSerializer.Deserialize<ResultList<Card>>(content);
				allCards.AddRange(cards!.Data);

				query = cards.NextPage;
				hasMore = cards.HasMore;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to load token cards from Scryfall");
				throw;
			}
		}

		return allCards.Where(c => c.TypeLine.StartsWith("Token"));
	}

	/// <summary> Returns all sets from Scryfall </summary>
	public async Task<IEnumerable<Set>> GetSetsAsync() => (await _api.Sets.Get()).Data;

	public IEnumerable<Set> GetSets() => _sets;
	public List<string> GetDoubleFacedCardNames() => _doubleFacedCardNames;

	public List<string> GetTokenCardNames() => _tokenCardNames;
}
