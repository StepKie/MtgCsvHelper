using System.Text.Json;
using ScryfallApi.Client;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

/// <summary>
/// Network fallback for Scryfall lookups not covered by <see cref="IReferenceCardCatalog"/>:
/// per-card resolution by cardmarket_id, cached in-process for the lifetime of the instance.
/// </summary>
public class CachedMtgApi : IMtgApi
{
	readonly Dictionary<int, Card> _cardsByCardmarketId = [];

	public CachedMtgApi() => Log.Debug("CachedMtgApi created");

	// Scryfall requires UserAgent and Accept headers since 09/2024.
	static readonly HttpClient HttpClient = new()
	{
		BaseAddress = ScryfallApiClientConfig.GetDefault().ScryfallApiBaseAddress,
		DefaultRequestHeaders =
				{
					{"User-Agent", AppInfo.UserAgent},
					{"Accept", "application/json"}
				}
	};

	// Scryfall's batch /cards/collection endpoint does NOT accept cardmarket_id as an identifier (only id,
	// mtgo_id, multiverse_id, oracle_id, illustration_id, name, name+set, set+collector_number). So we use
	// the per-card /cards/cardmarket/{id} endpoint sequentially, with the 50ms inter-request delay Scryfall asks for.
	const int InterRequestDelayMs = 50;
	static readonly JsonSerializerOptions ScryfallJsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};

	public async Task<IReadOnlyDictionary<int, Card>> GetCardsByCardmarketIdsAsync(IEnumerable<int> cardmarketIds)
	{
		var requested = cardmarketIds.Distinct().ToList();
		var notYetFetched = requested.Where(id => !_cardsByCardmarketId.ContainsKey(id)).ToList();

		for (int i = 0; i < notYetFetched.Count; i++)
		{
			var id = notYetFetched[i];
			try
			{
				var response = await HttpClient.GetAsync(new Uri($"https://api.scryfall.com/cards/cardmarket/{id}"));
				if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					continue; // not found — caller will see this id absent from the returned dictionary
				}
				response.EnsureSuccessStatusCode();
				var body = await response.Content.ReadAsStringAsync();
				var card = JsonSerializer.Deserialize<Card>(body, ScryfallJsonOptions);
				if (card is not null)
				{
					_cardsByCardmarketId[id] = card;
				}
			}
			catch (HttpRequestException ex)
			{
				Log.Warning(ex, $"Failed to resolve cardmarket_id {id}");
			}

			if (i + 1 < notYetFetched.Count)
			{
				await Task.Delay(InterRequestDelayMs);
			}
		}

		return requested
			.Where(_cardsByCardmarketId.ContainsKey)
			.ToDictionary(id => id, id => _cardsByCardmarketId[id]);
	}
}
