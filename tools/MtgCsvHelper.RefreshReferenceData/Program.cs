using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MtgCsvHelper;
using MtgCsvHelper.RefreshReferenceData;

// Two sub-commands:
//   (default)            refresh the bundled ReferenceCard catalog from Scryfall's default_cards.
//   cardmarket-fixture   regenerate Tests/cardmarket-reference-collection.csv from the moxfield reference.
//
// Usage:  dotnet run --project tools/MtgCsvHelper.RefreshReferenceData -- [<output-path>]
//         dotnet run --project tools/MtgCsvHelper.RefreshReferenceData -- cardmarket-fixture

if (args.Length > 0 && args[0] == "cardmarket-fixture")
{
	await CardmarketFixtureGenerator.RunAsync();
	return;
}

string defaultOutput = Path.GetFullPath(Path.Combine(
	AppContext.BaseDirectory, "..", "..", "..", "..", "..",
	"MtgCsvHelper.BlazorWebAssembly", "wwwroot", "data", "cards.min.json.gz"));

string outputPath = args.Length > 0 ? args[0] : defaultOutput;

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
http.DefaultRequestHeaders.UserAgent.ParseAdd(AppInfo.UserAgent);
http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

var serializerOptions = new JsonSerializerOptions
{
	PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	PropertyNameCaseInsensitive = true,
};

Console.WriteLine("Fetching /sets to build set_code → mtgo_code map…");
var setsResponse = await http.GetFromJsonAsync<ScryfallList<ScryfallSetJson>>("https://api.scryfall.com/sets", serializerOptions)
	?? throw new InvalidOperationException("Empty /sets response.");
if (setsResponse.HasMore)
{
	throw new InvalidOperationException("/sets returned a paginated response — implement pagination before the alias map silently truncates.");
}
var mtgoCodeBySet = setsResponse.Data
	.Where(s => !string.IsNullOrEmpty(s.MtgoCode))
	.ToDictionary(s => s.Code, s => s.MtgoCode!, StringComparer.OrdinalIgnoreCase);
Console.WriteLine($"  {mtgoCodeBySet.Count} sets carry an mtgo_code (of {setsResponse.Data.Count} total).");

Console.WriteLine("Looking up bulk-data manifest from Scryfall…");
var manifest = await http.GetFromJsonAsync<BulkDataManifest>("https://api.scryfall.com/bulk-data", serializerOptions)
	?? throw new InvalidOperationException("Empty bulk-data response.");
var defaultCards = manifest.Data.FirstOrDefault(d => d.Type == "default_cards")
	?? throw new InvalidOperationException("default_cards entry not found in bulk-data manifest.");

Console.WriteLine($"Downloading {defaultCards.Name} ({defaultCards.Size / 1e6:F1} MB) from {defaultCards.DownloadUri}…");
using var responseStream = await http.GetStreamAsync(defaultCards.DownloadUri);

int total = 0, kept = 0;
List<ReferenceCard> stripped = new(80_000);

await foreach (var card in JsonSerializer.DeserializeAsyncEnumerable<ScryfallCardJson>(responseStream, serializerOptions))
{
	total++;
	if (card is null) { continue; }
	mtgoCodeBySet.TryGetValue(card.Set, out var mtgoCode);
	stripped.Add(ReferenceCard.CreateFromScryfall(card, mtgoCode));
	kept++;
	if (total % 10_000 == 0) { Console.Write($"\r  parsed {total:N0} cards…"); }
}
Console.WriteLine($"\rParsed {total:N0} cards, kept {kept:N0}.");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
Console.WriteLine($"Writing gzipped bundle to {outputPath}…");

using (var fs = File.Create(outputPath))
using (var gz = new GZipStream(fs, CompressionLevel.SmallestSize))
{
	await JsonSerializer.SerializeAsync(gz, stripped, ReferenceCardCatalog.BundleSerializerOptions);
}

var size = new FileInfo(outputPath).Length;
Console.WriteLine($"Done. Bundle size: {size / 1024.0:F1} KB ({size / 1e6:F2} MB).");

internal sealed record BulkDataManifest(List<BulkDataEntry> Data);
internal sealed record BulkDataEntry(string Type, string Name, long Size,
	[property: JsonPropertyName("download_uri")] string DownloadUri);

internal sealed record ScryfallList<T>(List<T> Data, [property: JsonPropertyName("has_more")] bool HasMore);
internal sealed record ScryfallSetJson(string Code, [property: JsonPropertyName("mtgo_code")] string? MtgoCode);
