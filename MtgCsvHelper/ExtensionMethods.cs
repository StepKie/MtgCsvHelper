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
			_ => throw new ArgumentException($"No csv mapping registered for format {format}"),
		};
	}
}
