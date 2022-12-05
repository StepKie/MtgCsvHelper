namespace MtgCsvHelper.Models;

public record PhysicalMtgCard
{
	public int Count { get; init; }

	public required Printing Printing { get; init; }
	public double? PriceBought { get; init; }
	public DateTime? DateBought { get; init; }
	public required CardCondition Condition { get; init; }

	// TODO Might be enum or CultureInfo ...
	public string? Language { get; init; }
	public bool? Foil { get; init; }
	public bool? TradeListed { get; init; }
}
