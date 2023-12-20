using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper;

public class DeckFormat
{
	public static List<string> Supported { get; } = ["MOXFIELD", "DRAGONSHIELD", "MANABOX", "TOPDECKED", "DECKBOX"];
	public static List<string> NotYetFullySupported { get; } = ["CARDKINGDOM", "TCGPLAYER", "MTGGOLDFISH"];

	public static IEnumerable<DeckFormat> From(IConfiguration config) =>
		config.GetSection("CsvConfigurations")
		.GetChildren()
		.Select(c => new DeckFormat(config, c.Key));

	public DeckConfig ColumnConfig { get; }
	public string Name { get; }

	public DeckFormat(IConfiguration config, string configKey)
	{
		try
		{
			var section = config.GetSection($"CsvConfigurations:{configKey}");
			ColumnConfig = section.Get<DeckConfig>()!;
			Name = configKey;
		}
		catch (Exception ex)
		{
			Log.Error(ex, $"Could not load config {configKey}");
			throw;
		}
	}

	public CsvToCardMap GenerateClassMap() => new(ColumnConfig);

	public Currency Currency => Currency.FromString(ColumnConfig.PriceBought?.Currency);
}
