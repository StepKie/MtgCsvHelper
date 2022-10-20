public record PhysicalMtgCard
{
    public EditionMtgCard Card { get; set; }
    public double? PriceBought { get; set; }
    public DateTime? DateBought { get; set; }
    public CardCondition? Condition { get; set; }

    // TODO Might be enum or CultureInfo ...
    public string? Language { get; set; }
    public bool? Foil { get; set; }
    public bool? TradeListed { get; set; }
}