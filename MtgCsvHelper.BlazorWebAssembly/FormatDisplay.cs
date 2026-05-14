namespace MtgCsvHelper.BlazorWebAssembly;

public static class FormatDisplay
{
	static readonly Dictionary<string, string> _displayNames = new(StringComparer.OrdinalIgnoreCase)
	{
		["MOXFIELD"] = "Moxfield",
		["DRAGONSHIELD"] = "Dragon Shield",
		["MANABOX"] = "Manabox",
		["TOPDECKED"] = "Topdecked",
		["DECKBOX"] = "Deckbox",
		["MTGGOLDFISH"] = "MTG Goldfish",
		["CARDKINGDOM"] = "Card Kingdom",
		["CARDMARKET"] = "Cardmarket",
		["TCGPLAYER"] = "TCGplayer",
	};

	public static string For(string format) =>
		_displayNames.TryGetValue(format, out var name) ? name : format;
}
