namespace MtgCsvHelper;

public static class ExtensionMethods
{
	public static Type GetCsvMapType(this DeckFormat format)
	{
		return format switch
		{
			DeckFormat.DRAGONSHIELD => typeof(DragonShieldMap),
			DeckFormat.MOXFIELD => typeof(MoxfieldMap),
			DeckFormat.DECKBOX => typeof(DeckboxMap),
			DeckFormat.MANABOX => typeof(ManaboxMap),
			DeckFormat.TCGPLAYER => typeof(TcgPlayerMap),
			DeckFormat.CARDKINGDOM => typeof(CardKingdomMap),
			DeckFormat.UNKNOWN or _ => throw new ArgumentException($"No csv mapping registered for format {format}"),
		};
	}
}
