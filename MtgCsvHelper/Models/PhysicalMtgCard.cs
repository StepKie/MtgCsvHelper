namespace MtgCsvHelper.Models;

public record PhysicalMtgCard
{
	public int Count { get; set; }

	public required Printing Printing { get; set; }
	public double? PriceBought { get; set; }
	public DateTime? DateBought { get; set; }
	public required CardCondition Condition { get; set; }

	// TODO Might be enum or CultureInfo ...
	public string? Language { get; set; }
	public bool? Foil { get; set; }
	public bool? TradeListed { get; set; }
}
