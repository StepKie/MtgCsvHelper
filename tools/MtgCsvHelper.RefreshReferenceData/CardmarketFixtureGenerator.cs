using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper;
using MtgCsvHelper.Services;

namespace MtgCsvHelper.RefreshReferenceData;

// Generates cardmarket-reference-collection.csv by parsing moxfield-reference-collection.csv
// through the existing MOXFIELD pipeline, then looking up each card's cardmarket_id in the
// catalog and emitting a Cardmarket-shaped row. Re-run when the moxfield reference changes.
//
// Usage:  dotnet run --project tools/MtgCsvHelper.RefreshReferenceData -- cardmarket-fixture
internal static class CardmarketFixtureGenerator
{
	// Cardmarket idLanguage values, verified against MKM REST API v2.0 docs (Stock endpoint)
	// and the pymkm wrapper. Source: https://api.cardmarket.com/ws/documentation/API_2.0:Stock
	static readonly Dictionary<string, string> LanguageToId = new()
	{
		["en"] = "1",
		["fr"] = "2",
		["de"] = "3",
		["es"] = "4",
		["it"] = "5",
		["zhs"] = "6",
		["ja"] = "7",
		["pt"] = "8",
		["ru"] = "9",
		["ko"] = "10",
		["zht"] = "11",
	};

	public static async Task RunAsync()
	{
		var repoRoot = FindRepoRoot();
		var bundlePath = Path.Combine(repoRoot, "MtgCsvHelper.BlazorWebAssembly", "wwwroot", "data", "cards.min.json.gz");
		var appsettingsPath = Path.Combine(repoRoot, "MtgCsvHelper", "appsettings.json");
		var moxfieldPath = Path.Combine(repoRoot, "MtgCsvHelper", "Resources", "SampleCsvs", "Tests", "moxfield-reference-collection.csv");
		var outputPath = Path.Combine(repoRoot, "MtgCsvHelper", "Resources", "SampleCsvs", "Tests", "cardmarket-reference-collection.csv");

		Console.WriteLine($"Loading catalog from {bundlePath}…");
		await using var fs = File.OpenRead(bundlePath);
		var catalog = await ReferenceCardCatalog.LoadGzipAsync(fs);
		Console.WriteLine($"Catalog: {catalog.Count:N0} printings.");

		var config = new ConfigurationBuilder().AddJsonFile(appsettingsPath, optional: false).Build();
		var api = new CachedMtgApi();
		var resolver = new CardmarketResolver(() => catalog, api);
		var handler = new MtgCardCsvHandler(catalog, resolver, config, "MOXFIELD");

		Console.WriteLine($"Parsing {moxfieldPath}…");
		var result = await handler.ParseCollectionCsvAsync(moxfieldPath);
		Console.WriteLine($"Parsed {result.Collection.Cards.Count} cards ({result.ErrorCount} errors, {result.WarningCount} warnings).");

		var sb = new StringBuilder();
		sb.AppendLine("idProduct;groupCount;price;idLanguage;condition;isFoil;isSigned;isAltered;isPlayset;isReverseHolo;isFirstEd;isFullArt;isUberRare;isWithDie");
		int emitted = 0, skipped = 0;
		var unmappedLanguages = new HashSet<string>();

		foreach (var card in result.Collection.Cards)
		{
			// Catalog indexes set codes in lowercase Scryfall casing; parsed cards may be upper-case.
			// CA1308 suppression is correct here — Scryfall identifiers are ASCII lowercase by spec.
#pragma warning disable CA1308
			var setCode = card.Printing.Set.ToLowerInvariant();
#pragma warning restore CA1308
			var collectorNumber = card.Printing.CollectorNumber;
			var refCard = catalog.FindBySetAndCollectorNumber(setCode, collectorNumber);
			if (refCard?.CardmarketId is not int idProduct)
			{
				Console.WriteLine($"  skip: no cardmarket_id for {card.Printing.Name} ({setCode} #{collectorNumber})");
				skipped++;
				continue;
			}

			var languageKey = card.Language ?? "en";
			if (!LanguageToId.TryGetValue(languageKey, out var idLanguage))
			{
				unmappedLanguages.Add(languageKey);
				idLanguage = "1"; // safe fallback to English; flagged in the summary
			}

			// CardCondition.Id matches Cardmarket's integer condition encoding exactly
			// (1=Mint, 2=NearMint, …, 7=Poor — natural ordering of the MT/NM/EX/GD/LP/PL/PO codes).
			var condition = ((int)card.Condition.Id).ToString(CultureInfo.InvariantCulture);
			var isFoil = card.Foil == true ? "1" : "";
			var groupCount = card.Count.ToString(CultureInfo.InvariantCulture);
			var price = card.PriceBought is { Value: > 0 } money
				? money.Value.ToString("0.00", CultureInfo.InvariantCulture)
				: "";

			sb.Append(idProduct).Append(';')
				.Append(groupCount).Append(';')
				.Append(price).Append(';')
				.Append(idLanguage).Append(';')
				.Append(condition).Append(';')
				.Append(isFoil)
				.AppendLine(";;;;;;;;");
			emitted++;
		}

		await File.WriteAllTextAsync(outputPath, sb.ToString());

		Console.WriteLine();
		Console.WriteLine($"Wrote {emitted} rows to {outputPath}");
		Console.WriteLine($"Skipped {skipped} cards with no cardmarket_id.");
		if (unmappedLanguages.Count > 0)
		{
			Console.WriteLine($"Warning: unmapped languages defaulted to English: {string.Join(", ", unmappedLanguages)}");
		}
	}

	// Walks up from the tool's bin/ output directory to find the repo root (identified by .slnx).
	static string FindRepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MtgCsvHelper.slnx")))
		{
			dir = dir.Parent;
		}
		return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (MtgCsvHelper.slnx) above " + AppContext.BaseDirectory);
	}
}
