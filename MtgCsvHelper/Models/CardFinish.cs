namespace MtgCsvHelper.Models;

/// <summary>
/// Physical print finish, matching Scryfall's closed <c>finishes</c> vocabulary. A copy is
/// exactly one of these. Foiling <em>treatments</em> (Rainbow Foil, Surge Foil, …) are a
/// separate Scryfall axis (<c>promo_types</c>) and collapse to <see cref="Foil"/> here.
/// </summary>
public enum CardFinish
{
	Unknown,
	Normal,
	Foil,
	Etched,
}
