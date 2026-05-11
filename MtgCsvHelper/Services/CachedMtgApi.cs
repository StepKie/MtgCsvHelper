using System.Text.Json;
using ScryfallApi.Client;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

/// <summary>
/// Network fallback for Scryfall lookups not covered by <see cref="IReferenceCardCatalog"/>:
/// per-card resolution by cardmarket_id, cached in-process for the lifetime of the instance.
/// Scryfall's response shape is mapped to <see cref="ReferenceCard"/> on the way out so callers
/// see only canonical types.
/// </summary>
public class CachedMtgApi : IMtgApi
{
	readonly Dictionary<int, ReferenceCard> _cardsByCardmarketId = [];

	public CachedMtgApi() => Log.Debug("CachedMtgApi created");

	// Scryfall requires UserAgent and Accept headers since 09/2024.
	static readonly HttpClient ScryfallHttpClient = new()
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

	public async Task<IReadOnlyDictionary<int, ReferenceCard>> GetCardsByCardmarketIdsAsync(IEnumerable<int> cardmarketIds)
	{
		var requested = cardmarketIds.Distinct().ToList();
		var notYetFetched = requested.Where(id => !_cardsByCardmarketId.ContainsKey(id)).ToList();

		for (int i = 0; i < notYetFetched.Count; i++)
		{
			var id = notYetFetched[i];
			try
			{
				var response = await ScryfallHttpClient.GetAsync(new Uri($"https://api.scryfall.com/cards/cardmarket/{id}"));
				if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					continue; // not found — caller will see this id absent from the returned dictionary
				}
				response.EnsureSuccessStatusCode();
				var body = await response.Content.ReadAsStringAsync();
				var card = JsonSerializer.Deserialize<Card>(body, ScryfallJsonOptions);
				if (card is not null)
				{
					_cardsByCardmarketId[id] = MapToReferenceCard(card);
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

	// Mirror of the same field choices made by the bundle generator (tools/MtgCsvHelper.RefreshReferenceData):
	// strip a Scryfall printing to the fields ReferenceCard exposes, with the same defaults (Lang/Layout/Finishes).
	// Card.CardMarketId / TcgplayerId / TcgplayerEtchedId are non-nullable ints in the Scryfall library and use 0
	// as the "unset" sentinel; we collapse 0 -> null on the way out so consumers can distinguish "no id" from "id 0".
	static ReferenceCard MapToReferenceCard(Card c) => new(
		Id: c.Id,
		OracleId: Guid.TryParse(c.OracleId, out var oid) ? oid : null,
		Name: c.Name,
		Set: c.Set,
		SetName: c.SetName,
		CollectorNumber: c.CollectorNumber,
		Lang: string.IsNullOrEmpty(c.Language) ? "en" : c.Language,
		Layout: string.IsNullOrEmpty(c.Layout) ? "normal" : c.Layout,
		Finishes: c.Finishes ?? [],
		FrameEffects: c.FrameEffects,
		BorderColor: c.BorderColor,
		PromoTypes: c.PromoTypes,
		CardmarketId: c.CardMarketId > 0 ? c.CardMarketId : null,
		TcgplayerId: c.TcgplayerId > 0 ? c.TcgplayerId : null,
		TcgplayerEtchedId: c.TcgplayerEtchedId > 0 ? c.TcgplayerEtchedId : null,
		MultiverseIds: c.MultiverseIds);
}
