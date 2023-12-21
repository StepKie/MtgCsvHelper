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
	}

	public async Task<List<string>> GetDoubleFacedCardNamesAsync()
	{
		var cards = await _api.Catalogs.ListCardNames();
		var names = cards.Where(name => name.Contains(" // ")).ToList() ?? [];

		Console.WriteLine($"Loaded {names.Count} double-faced cards from server");
		Log.Debug($"Loaded {names.Count} double-faced cards from server");

		return names;
	}

	/// <summary> Returns all sets from Scryfall </summary>
	public async Task<IEnumerable<Set>> GetSetsAsync() => (await _api.Sets.Get()).Data;

	public IEnumerable<Set> GetSets() => _sets;
	public List<string> GetDoubleFacedCardNames() => _doubleFacedCardNames;
}
