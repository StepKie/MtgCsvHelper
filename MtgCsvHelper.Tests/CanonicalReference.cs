using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Services;

namespace MtgCsvHelper.Tests;

/// <summary>
/// master.csv holds the lossless reference collection in the CANONICAL format — defined in
/// appsettings.json with one column per model field and canonical values, so it represents every
/// dimension without loss. CANONICAL is the single source of truth's serialization; it is kept out
/// of CardMapFactory.Supported, so it never appears in the UI or auto-detection. Every real-format
/// sample CSV is generated from master.csv.
/// </summary>
internal static class CanonicalReference
{
	public const string FormatName = "CANONICAL";
	public static readonly string MasterCsvPath = Path.Combine(RepoRoot(), "MtgCsvHelper.Tests", "Resources", "master.csv");

	/// <summary>
	/// Parses master.csv through the full pipeline (so Rarity/SetName backfill exactly as for any
	/// import). Prices are re-stamped to the requested currency — the master holds canonical USD, but
	/// the value is what matters and each consuming format dictates its own currency.
	/// </summary>
	public static List<PhysicalMtgCard> LoadCards(IConfiguration config, IReferenceCardCatalog catalog, ICardmarketResolver resolver, Currency currency)
	{
		var handler = new MtgCardCsvHandler(catalog, resolver, config, FormatName);
		var cards = handler.ParseCollectionCsv(MasterCsvPath).Collection.Cards;

		return cards
			.Select(c => c with { PriceBought = c.PriceBought is { } money ? money with { Currency = currency } : null })
			.ToList();
	}

	static string RepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MtgCsvHelper.slnx")))
		{
			dir = dir.Parent;
		}

		return dir?.FullName ?? throw new InvalidOperationException($"Could not locate repo root (MtgCsvHelper.slnx) above {AppContext.BaseDirectory}");
	}
}
