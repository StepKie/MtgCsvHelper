using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper;

public class CardMapFactory(IConfiguration config, IReferenceCardCatalog catalog)
{
	readonly List<FormatConfig> _formatConfigs = From(config).ToList();

	public static IReadOnlyList<string> Supported { get; } = ["MOXFIELD", "DRAGONSHIELD", "MANABOX", "TOPDECKED", "DECKBOX", "CARDKINGDOM", "MTGGOLDFISH", "TCGPLAYER", "CARDMARKET", "ARCHIDEKT"];
	public static IReadOnlyList<string> NotYetFullySupported { get; } = ["URZAGATHERER"];

	// Formats whose CSV doesn't carry enough info to populate a complete card without external lookups,
	// or that don't make sense as targets for our writers. Internal so tests can derive expectations
	// from the same source of truth as the Readable/Writable filters below.
	internal static readonly HashSet<string> WriteOnlyFormats = new(StringComparer.OrdinalIgnoreCase) { "CARDKINGDOM" };
	internal static readonly HashSet<string> ReadOnlyFormats = new(StringComparer.OrdinalIgnoreCase) { "CARDMARKET" };

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

	public FormatConfig? GetFormatConfig(string format) => _formatConfigs.FirstOrDefault(c => c.Name.Equals(format, StringComparison.OrdinalIgnoreCase));

	public ClassMap<PhysicalMtgCard> GenerateReadMap(string format)
	{
		var cfg = GetRequiredFormatConfig(format);
		if (WriteOnlyFormats.Contains(format))
		{
			throw new InvalidOperationException($"Format '{format}' is write-only and cannot be parsed.");
		}
		return new PhysicalCardMap(cfg, catalog);
	}

	public ClassMap<PhysicalMtgCard> GenerateWriteMap(string format)
	{
		var cfg = GetRequiredFormatConfig(format);
		if (ReadOnlyFormats.Contains(format))
		{
			throw new InvalidOperationException($"Format '{format}' is read-only and cannot be written.");
		}
		return format.Equals("CARDKINGDOM", StringComparison.OrdinalIgnoreCase)
			? new CardKingdomWriteMap(cfg, catalog)
			: new PhysicalCardMap(cfg, catalog);
	}

	// Throwing counterpart to GetFormatConfig (matches the GetService/GetRequiredService convention).
	// Two distinct failure modes, in order of precondition:
	//   1. _formatConfigs is empty       — appsettings.json never loaded; system is misconfigured.
	//   2. format isn't in _formatConfigs — caller asked for something we don't know.
	FormatConfig GetRequiredFormatConfig(string format)
	{
		if (_formatConfigs.Count == 0)
		{
			throw new InvalidOperationException(
				"No format configurations loaded. Check that appsettings.json is reachable and contains a 'CsvConfigurations' section.");
		}

		return GetFormatConfig(format)
			?? throw new ArgumentException(
				$"Unsupported format '{format}'. Loaded: {string.Join(", ", _formatConfigs.Select(c => c.Name))}.",
				nameof(format));
	}
}
