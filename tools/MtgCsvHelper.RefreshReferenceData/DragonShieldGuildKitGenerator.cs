using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MtgCsvHelper;

namespace MtgCsvHelper.RefreshReferenceData;

// Generates MtgCsvHelper/Resources/dragonshield-guildkit-codes.json: a
// (Scryfall guild-kit coordinate) -> (Dragon Shield native set code) table.
//
// Dragon Shield's CSV importer ignores the canonical gk1/gk2 set codes and falls back to
// name-only matching, silently landing reprints on the wrong edition. It DOES honor its own
// proprietary GK<n>_<GUILD> codes (GK2_AZORIU, GK1_DIMIR, ...). The guild is recoverable from
// each printing's Scryfall `watermark` (azorius, dimir, ...), truncated to Dragon Shield's
// 6-char form. We emit one entry per (set, collector_number) so the Dragon Shield writer can
// look the native code up without carrying watermark on every ReferenceCard in the bundle.
//
// Cards with no watermark (a handful of guild-agnostic reprints — Char, Birds of Paradise, ...)
// are skipped; the writer falls back to the canonical gk1/gk2 for those (same as today).
//
// Output shape: { "gk1/1": "GK1_DIMIR", "gk2/2": "GK2_AZORIU", ... }
//
// Usage:  dotnet run --project tools/MtgCsvHelper.RefreshReferenceData -- dragonshield-guildkit
internal static class DragonShieldGuildKitGenerator
{
	// Dragon Shield truncates the guild name to 6 chars (azorius -> AZORIU, selesnya -> SELESN).
	const int GuildCodeLength = 6;

	static readonly string[] GuildKitSets = ["gk1", "gk2"];

	public static async Task RunAsync()
	{
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
		http.DefaultRequestHeaders.UserAgent.ParseAdd(AppInfo.UserAgent);
		http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

		var codes = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		int withWatermark = 0, skipped = 0;

		foreach (var set in GuildKitSets)
		{
			var cards = await FetchSetPrintings(http, set);
			Console.WriteLine($"  {set}: {cards.Count} printings.");
			foreach (var card in cards)
			{
				if (string.IsNullOrEmpty(card.Watermark)) { skipped++; continue; }

				var guild = card.Watermark.ToUpperInvariant();
				var native = $"{set.ToUpperInvariant()}_{guild[..Math.Min(GuildCodeLength, guild.Length)]}";
				codes[$"{set}/{card.CollectorNumber}"] = native;
				withWatermark++;
			}
		}

		Console.WriteLine($"  Mapped {withWatermark} guild-kit printings; skipped {skipped} watermark-less reprints.");

		var resourcesDir = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory, "..", "..", "..", "..", "..",
			"MtgCsvHelper", "Resources"));
		Directory.CreateDirectory(resourcesDir);
		var jsonOpts = new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};
		var outputPath = Path.Combine(resourcesDir, "dragonshield-guildkit-codes.json");
		await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(codes, jsonOpts) + Environment.NewLine);
		Console.WriteLine($"Wrote {codes.Count} guild-kit codes to {outputPath}.");
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
