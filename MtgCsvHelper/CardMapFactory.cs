using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Maps;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;

public class CardMapFactory(IConfiguration config, IMtgApi api)
{
	readonly List<FormatConfig> _formatConfigs = From(config).ToList();

	public static List<string> Supported { get; } = ["MOXFIELD", "DRAGONSHIELD", "MANABOX", "TOPDECKED", "DECKBOX", "CARDKINGDOM", "MTGGOLDFISH", "TCGPLAYER"];
	public static List<string> NotYetFullySupported { get; } = ["URZAGATHERER"];

	// Formats that cannot be parsed (their CSV does not carry enough information to recover a canonical card).
	static readonly HashSet<string> WriteOnlyFormats = new(StringComparer.OrdinalIgnoreCase) { "CARDKINGDOM" };

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
		return new PhysicalCardMap(cfg, api);
	}

	public ClassMap<PhysicalMtgCard> GenerateWriteMap(string format)
	{
		var cfg = RequireFormat(format);
		return format.Equals("CARDKINGDOM", StringComparison.OrdinalIgnoreCase)
			? new CardKingdomWriteMap(cfg, api)
			: new PhysicalCardMap(cfg, api);
	}

	FormatConfig RequireFormat(string format) => GetFormatConfig(format)
		?? throw new ArgumentException($"Unsupported format '{format}'. Supported: {string.Join(", ", Supported)}.", nameof(format));
}
