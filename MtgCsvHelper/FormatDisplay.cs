namespace MtgCsvHelper;

/// <summary>
/// Maps the uppercase format identifiers used in <c>appsettings.json</c> to user-friendly
/// display strings. Unknown keys fall back to the identifier itself (safe but ugly), so
/// adding a new format in <c>CsvConfigurations</c> means adding a row here too. Lives in
/// the core library (not Blazor) so any UI — and the test project — can use it.
/// </summary>
public static class FormatDisplay
{
	// Exposed as internal IReadOnlyDictionary so the test project (InternalsVisibleTo) can
	// assert every CardMapFactory.Supported format has a display entry — the ARCHIDEKT
	// label-bug we shipped in 1.3.0 would have been caught by that test.
	internal static readonly IReadOnlyDictionary<string, string> DisplayNames =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
		DisplayNames.TryGetValue(format, out var name) ? name : format;
}
