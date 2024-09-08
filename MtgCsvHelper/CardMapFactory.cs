using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper;

public class CardMapFactory(IConfiguration config)
{
	readonly List<DeckConfig> _deckConfigs = From(config).ToList();

	public static List<string> Supported { get; } = ["MOXFIELD", "DRAGONSHIELD", "MANABOX", "TOPDECKED", "DECKBOX", "CARDKINGDOM", "MTGGOLDFISH"];
	public static List<string> NotYetFullySupported { get; } = ["TCGPLAYER"];

	public static IEnumerable<DeckConfig> From(IConfiguration config) =>
		config.GetSection("CsvConfigurations")
		.GetChildren()
		.Select(c => c.Get<DeckConfig>()!);

	public DefaultCollectionEntryMap? GenerateClassMap(string format) => GetDeckConfig(format) is DeckConfig dc ? new(dc, format.Equals("CARDKINGDOM")) : null;

	private DeckConfig? GetDeckConfig(string format) => _deckConfigs.FirstOrDefault(c => c.Name.Equals(format));
}
