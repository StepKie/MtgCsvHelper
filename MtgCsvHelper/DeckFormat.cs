using DevTrends.ConfigurationExtensions;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper;

public class DeckFormat
{
	public static List<string> Supported { get; } = ["MOXFIELD", "DRAGONSHIELD", "MANABOX", "TOPDECKED", "DECKBOX"];
	public static List<string> NotYetFullySupported { get; } = ["CARDKINGDOM", "TCGPLAYER", "MTGGOLDFISH"];

	public static IEnumerable<DeckFormat> From(IConfiguration config) => config.GetSection("CsvConfigurations").GetChildren().Select(c => new DeckFormat(config, c.Key));

	public static IEnumerable<DeckFormat> FromConfig()
	{
		IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build();
		return config.GetSection("CsvConfigurations").GetChildren().Select(c => new DeckFormat(config, c.Key));
	}

	public DeckConfig ColumnConfig { get; }
	public string Name { get; }

	public DeckFormat(IConfiguration config, string configKey)
	{
		ColumnConfig = config.Bind<DeckConfig>($"CsvConfigurations:{configKey}") ?? throw new ArgumentException($"Config {configKey} not found in appsettings.json");
		Name = configKey;
	}

	public CsvToCardMap GenerateClassMap() => new(ColumnConfig);

	public Currency Currency => Currency.FromString(ColumnConfig.PriceBought?.Currency);
}
