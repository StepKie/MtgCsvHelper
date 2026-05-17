namespace MtgCsvHelper.BlazorWebAssembly;

/// <summary>
/// Maps the uppercase format identifiers used in <c>appsettings.json</c> to user-friendly
/// display strings. Unknown keys fall back to the identifier itself (safe but ugly), so
/// adding a new format in <c>CsvConfigurations</c> means adding a row here too.
/// </summary>
public static class FormatDisplay
{
	static readonly Dictionary<string, string> _displayNames = new(StringComparer.OrdinalIgnoreCase)
	{
		["MOXFIELD"] = "Moxfield",
		["DRAGONSHIELD"] = "Dragon Shield",
		["MANABOX"] = "Manabox",
		["TOPDECKED"] = "Topdecked",
		["DECKBOX"] = "Deckbox",
		["MTGGOLDFISH"] = "MTGGoldfish",
		["CARDKINGDOM"] = "Card Kingdom",
		["CARDMARKET"] = "Cardmarket",
		["TCGPLAYER"] = "TCGplayer",
		["ARCHIDEKT"] = "Archidekt",
	};

	public static string For(string format) =>
		_displayNames.TryGetValue(format, out var name) ? name : format;
}
