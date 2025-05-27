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
		Log.Debug("CachedMtgApi created");
	}

	/// <summary> Default Client to use when accessing Scryfall API. Since 09/2024, required UserAgent and Accept headers to be set </summary>
	public static HttpClient DefaultClient => new HttpClient()
	{
		BaseAddress = ScryfallApiClientConfig.GetDefault().ScryfallApiBaseAddress,
		DefaultRequestHeaders =
				{
					{"User-Agent", "MtgCsvHelper/1.0.0"},
					{"Accept", "application/json"}
				}
	};

	public async Task LoadData()
	{
		Log.Debug($"Scryfall - Syncing data...");
		_sets ??= (await GetSetsAsync()).ToList();

		_doubleFacedCardNames ??= await GetDoubleFacedCardNamesAsync();
		Log.Debug($"Scryfall - Loaded {_doubleFacedCardNames.Count} double-faced cards.");

		_tokenCardNames ??= (await GetTokenCardNamesAsync()).Select(c => c.Name).Distinct().ToList();

		Log.Debug($"Scryfall - Loaded {_tokenCardNames.Count} tokens.");
		Log.Debug($"Scryfall - Sync complete.");
	}

	public async Task<List<string>> GetDoubleFacedCardNamesAsync()
	{
		var cards = await _api.Catalogs.ListCardNames();
		var names = cards.Where(name => name.Contains(" // ")).ToList() ?? [];

		return names;
	}

	public async Task<IEnumerable<Card>> GetTokenCardNamesAsync()
	{
		// Unfortunately, the IScryfallApiClient has an issue with encoding the query string, so we use our own HttpClient
		var httpClient = CachedMtgApi.DefaultClient;
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
