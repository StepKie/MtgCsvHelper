using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Models;

public record PhysicalMtgCard
{
	public int Count { get; init; }

	public required Card Printing { get; init; }
	public decimal? PriceBought { get; init; }
	public DateTime? DateBought { get; init; }
	public CardCondition Condition { get; init; } = CardCondition.UNKNOWN;

	// TODO Might be enum or CultureInfo ...
	public string? Language { get; init; }
	public bool? Foil { get; init; }
	public bool? TradeListed { get; init; }
}
