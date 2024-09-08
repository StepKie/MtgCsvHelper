﻿using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Models;

public record PhysicalMtgCard
{
	public required Card Printing { get; init; }
	public Money? PriceBought { get; init; }
	public DateTime? DateBought { get; init; }
	public CardCondition Condition { get; init; } = CardCondition.UNKNOWN;

	// Encodes 2-letter ISO language code (queryable via CultureInfo)
	public string? Language { get; init; }
	public bool? Foil { get; init; }
	public bool? TradeListed { get; init; }
}
