namespace MtgCsvHelper.Models;

/// <summary> Scryfall printing rarity, ordered as Scryfall documents it after <see cref="Unknown"/>. </summary>
public enum CardRarity
{
	Unknown,
	Common,
	Uncommon,
	Rare,
	Special,
	Mythic,
	Bonus,
}
