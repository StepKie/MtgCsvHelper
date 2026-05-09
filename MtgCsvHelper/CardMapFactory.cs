using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Maps;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;

public class CardMapFactory(IConfiguration config, IMtgApi api)
{
	readonly List<FormatConfig> _formatConfigs = From(config).ToList();

	public static List<string> Supported { get; } = ["MOXFIELD", "DRAGONSHIELD", "MANABOX", "TOPDECKED", "DECKBOX", "CARDKINGDOM", "MTGGOLDFISH", "TCGPLAYER", "CARDMARKET"];
	public static List<string> NotYetFullySupported { get; } = ["URZAGATHERER"];

	// Formats whose CSV doesn't carry enough info to populate a complete card without external lookups,
	// or that don't make sense as targets for our writers.
	static readonly HashSet<string> WriteOnlyFormats = new(StringComparer.OrdinalIgnoreCase) { "CARDKINGDOM" };
	static readonly HashSet<string> ReadOnlyFormats = new(StringComparer.OrdinalIgnoreCase) { "CARDMARKET" };

	public static IEnumerable<FormatConfig> From(IConfiguration config) =>
		config.GetSection("CsvConfigurations")
		.GetChildren()
		.Select(c => c.Get<FormatConfig>()!);

	public FormatConfig? GetFormatConfig(string format) => _formatConfigs.FirstOrDefault(c => c.Name.Equals(format, StringComparison.OrdinalIgnoreCase));

	public ClassMap<PhysicalMtgCard> GenerateReadMap(string format)
	{
		var cfg = RequireFormat(format);
		if (WriteOnlyFormats.Contains(format))
		{
			throw new InvalidOperationException($"Format '{format}' is write-only and cannot be parsed.");
		}
		ValidateCardIdentifier(cfg);
		return new PhysicalCardMap(cfg, api);
	}

	public ClassMap<PhysicalMtgCard> GenerateWriteMap(string format)
	{
		var cfg = RequireFormat(format);
		if (ReadOnlyFormats.Contains(format))
		{
			throw new InvalidOperationException($"Format '{format}' is read-only and cannot be written.");
		}
		ValidateCardIdentifier(cfg);
		return format.Equals("CARDKINGDOM", StringComparison.OrdinalIgnoreCase)
			? new CardKingdomWriteMap(cfg, api)
			: new PhysicalCardMap(cfg, api);
	}

	FormatConfig RequireFormat(string format) => GetFormatConfig(format)
		?? throw new ArgumentException($"Unsupported format '{format}'. Supported: {string.Join(", ", Supported)}.", nameof(format));

	static void ValidateCardIdentifier(FormatConfig cfg)
	{
		if (cfg.CardName is null && cfg.CardmarketId is null)
		{
			throw new InvalidOperationException($"Format '{cfg.Name}' must specify either a CardName or a CardmarketId column.");
		}
	}
}
