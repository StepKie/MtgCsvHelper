using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MtgCsvHelper;

namespace MtgCsvHelper.RefreshReferenceData;

/// <summary>
/// Generates <c>MtgCsvHelper/Resources/dragonshield-guildkit-editions.json</c>: a Scryfall guild-kit
/// coordinate → Dragon Shield edition-name table. Dragon Shield splits each Ravnica guild kit into
/// per-guild editions (<c>Guild Kit: Azorius</c>, <c>Guild Kit: Dimir</c>, …) and resolves imports by
/// that Set Name, not the Set Code — so a card sent as <c>gk2</c> / <c>RNA Guild Kit</c> mis-resolves.
/// The guild comes from each printing's Scryfall <c>watermark</c>; we emit one entry per
/// (set, collector_number) so the writer can emit the native edition name. Cards with no watermark
/// (guild-agnostic reprints — Char, Birds of Paradise, …) are skipped, and the writer keeps the
/// canonical set name for those. Output shape: <c>{ "gk1/1": "Guild Kit: Dimir", "gk2/2": "Guild Kit: Azorius", … }</c>.
/// Run via <c>dotnet run --project tools/MtgCsvHelper.RefreshReferenceData -- dragonshield-guildkit</c>.
/// </summary>
internal static class DragonShieldGuildKitGenerator
{
	static readonly string[] GuildKitSets = ["gk1", "gk2"];

	public static async Task RunAsync()
	{
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
		http.DefaultRequestHeaders.UserAgent.ParseAdd(AppInfo.UserAgent);
		http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

		var editions = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		int withWatermark = 0, skipped = 0;

		foreach (var set in GuildKitSets)
		{
			var cards = await FetchSetPrintings(http, set);
			Console.WriteLine($"  {set}: {cards.Count} printings.");
			foreach (var card in cards)
			{
				if (string.IsNullOrEmpty(card.Watermark)) { skipped++; continue; }

				// Watermark is lowercase ("azorius"); Dragon Shield titles it ("Guild Kit: Azorius").
				var guild = char.ToUpperInvariant(card.Watermark[0]) + card.Watermark[1..];
				editions[$"{set}/{card.CollectorNumber}"] = $"Guild Kit: {guild}";
				withWatermark++;
			}
		}

		Console.WriteLine($"  Mapped {withWatermark} guild-kit printings; skipped {skipped} watermark-less reprints.");

		var resourcesDir = Path.Combine(RepoRoot.Find(), "MtgCsvHelper", "Resources");
		Directory.CreateDirectory(resourcesDir);
		var jsonOpts = new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};
		var outputPath = Path.Combine(resourcesDir, "dragonshield-guildkit-editions.json");
		await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(editions, jsonOpts) + Environment.NewLine);
		Console.WriteLine($"Wrote {editions.Count} guild-kit editions to {outputPath}.");
	}

	static async Task<List<GuildKitCard>> FetchSetPrintings(HttpClient http, string set)
	{
		var serializerOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			PropertyNameCaseInsensitive = true,
		};

		var all = new List<GuildKitCard>();
		var url = $"https://api.scryfall.com/cards/search?q=set%3A{set}&unique=prints";
		while (url is not null)
		{
			var page = await http.GetFromJsonAsync<ScryfallSearchPage>(url, serializerOptions)
				?? throw new InvalidOperationException($"Empty search response for set:{set}.");
			all.AddRange(page.Data);
			url = page.HasMore ? page.NextPage : null;
		}

		return all;
	}

	internal sealed record GuildKitCard(
		[property: JsonPropertyName("collector_number")] string CollectorNumber,
		string? Watermark);

	internal sealed record ScryfallSearchPage(
		List<GuildKitCard> Data,
		[property: JsonPropertyName("has_more")] bool HasMore,
		[property: JsonPropertyName("next_page")] string? NextPage);
}
