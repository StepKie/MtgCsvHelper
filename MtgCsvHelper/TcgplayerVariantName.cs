namespace MtgCsvHelper;

/// <summary>
/// Maps a printing's Scryfall attributes to the parenthetical variant suffix TCGplayer appends to
/// a card name (e.g. "Orcish Bowmasters (Borderless)"). TCGplayer's importer matches by name, so a
/// borderless/showcase/extended-art printing only resolves if the suffix is present — the collector
/// number alone isn't enough.
/// </summary>
public static class TcgplayerVariantName
{
	/// <summary>The TCGplayer variant suffix for a printing (e.g. <c>"(Borderless)"</c>), or null if it's a normal printing.</summary>
	public static string? SuffixFor(ReferenceCard card)
	{
		// Order is TCGplayer's precedence when a printing carries more than one treatment.
		if (string.Equals(card.BorderColor, "borderless", StringComparison.OrdinalIgnoreCase)) { return "(Borderless)"; }
		if (HasFrameEffect(card, "showcase")) { return "(Showcase)"; }
		if (HasFrameEffect(card, "extendedart")) { return "(Extended Art)"; }

		return null;
	}

	/// <summary><paramref name="baseName"/> with the variant suffix appended when the printing has one.</summary>
	public static string Decorate(string baseName, ReferenceCard card) =>
		SuffixFor(card) is { } suffix ? $"{baseName} {suffix}" : baseName;

	static bool HasFrameEffect(ReferenceCard card, string effect) =>
		card.FrameEffects?.Any(e => string.Equals(e, effect, StringComparison.OrdinalIgnoreCase)) == true;
}
