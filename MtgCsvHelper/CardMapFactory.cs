using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper;

public class CardMapFactory(IConfiguration config, IReferenceCardCatalog catalog)
{
	readonly List<FormatConfig> _formatConfigs = LoadOrThrow(config);

	public static IReadOnlyList<string> Supported { get; } = ["MOXFIELD", "DRAGONSHIELD", "MANABOX", "TOPDECKED", "DECKBOX", "CARDKINGDOM", "MTGGOLDFISH", "TCGPLAYER", "CARDMARKET", "ARCHIDEKT", "MTGO"];

	// Write-only / read-only format sets; internal so tests derive expectations from the same source of truth.
	internal static readonly IReadOnlySet<string> WriteOnlyFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CARDKINGDOM" };
	internal static readonly IReadOnlySet<string> ReadOnlyFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CARDMARKET" };

	public static IReadOnlyList<string> ReadableFormats { get; } = [.. Supported.Where(f => !WriteOnlyFormats.Contains(f))];
	public static IReadOnlyList<string> WritableFormats { get; } = [.. Supported.Where(f => !ReadOnlyFormats.Contains(f))];

	public static IEnumerable<FormatConfig> From(IConfiguration config) =>
		config.GetSection("CsvConfigurations")
		.GetChildren()
		.Select(c =>
		{
			var cfg = c.Get<FormatConfig>()!;
			cfg.Validate(); // fail fast at load: any malformed format blocks startup, not just first use
			return cfg;
		});

	// Excludes internal formats like CANONICAL — they live in appsettings.json but must never be auto-detected or offered to users.
	public static IEnumerable<FormatConfig> SupportedConfigs(IConfiguration config) =>
		From(config).Where(c => Supported.Contains(c.Name, StringComparer.OrdinalIgnoreCase));

	public FormatConfig? GetFormatConfig(string format) => _formatConfigs.FirstOrDefault(c => c.Name.Equals(format, StringComparison.OrdinalIgnoreCase));

	public ClassMap<PhysicalMtgCard> GenerateReadMap(string format)
	{
		var cfg = GetRequiredFormatConfig(format);
		if (WriteOnlyFormats.Contains(format))
		{
			throw new InvalidOperationException($"Format '{format}' is write-only and cannot be parsed.");
		}

		return format.ToUpperInvariant() switch
		{
			"DECKBOX" => new DeckboxMap(cfg, catalog),
			"DRAGONSHIELD" => new DragonShieldMap(cfg, catalog),
			_ => new PhysicalCardMap(cfg, catalog),
		};
	}

	public ClassMap<PhysicalMtgCard> GenerateWriteMap(string format)
	{
		var cfg = GetRequiredFormatConfig(format);
		if (ReadOnlyFormats.Contains(format))
		{
			throw new InvalidOperationException($"Format '{format}' is read-only and cannot be written.");
		}

		return format.ToUpperInvariant() switch
		{
			"CARDKINGDOM" => new CardKingdomWriteMap(cfg, catalog),
			"DECKBOX" => new DeckboxMap(cfg, catalog),
			"DRAGONSHIELD" => new DragonShieldMap(cfg, catalog),
			"TCGPLAYER" => new TCGPlayerWriteMap(cfg, catalog),
			_ => new PhysicalCardMap(cfg, catalog),
		};
	}

	// Throwing counterpart to GetFormatConfig for callers that can't proceed without one.
	internal FormatConfig GetRequiredFormatConfig(string format) =>
		GetFormatConfig(format)
			?? throw new ArgumentException(
				$"Unsupported format '{format}'. Loaded: {string.Join(", ", _formatConfigs.Select(c => c.Name))}.",
				nameof(format));

	// Fail fast at construction: an empty config means appsettings.json never loaded — no later call can succeed.
	static List<FormatConfig> LoadOrThrow(IConfiguration config)
	{
		var configs = From(config).ToList();
		if (configs.Count == 0)
		{
			throw new InvalidOperationException(
				"No format configurations loaded. Check that appsettings.json is reachable and contains a 'CsvConfigurations' section.");
		}

		return configs;
	}
}
