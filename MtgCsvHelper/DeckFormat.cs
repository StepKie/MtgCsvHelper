using DevTrends.ConfigurationExtensions;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper;

public class DeckFormat
{
	public static List<string> CardNames { get; private set; }

	public DeckConfig ColumnConfig { get; }
	public string Name { get; }

	public DeckFormat(IConfiguration config, string configKey)
	{
		ColumnConfig = config?.Bind<DeckConfig>($"CsvConfigurations:{configKey}") ?? throw new ArgumentException($"Config {configKey} not found in appsettings.json");
		Name = configKey;

		// Set only once!
		CardNames ??= config.GetSection("DoubleFacedCards").Get<List<string>>();
	}

	public CsvToCardMap GenerateClassMap() => new(ColumnConfig);
}
