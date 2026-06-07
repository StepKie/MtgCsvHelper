namespace MtgCsvHelper.Models;

/// <summary>
/// Aggregate statistics over a collection of <see cref="PhysicalMtgCard"/>. Computed once
/// after parse; consumers (UI, logging, Console output) read fields directly instead of
/// recomputing.
/// </summary>
/// <param name="TotalCount">Sum of <see cref="PhysicalMtgCard.Count"/> across all cards.</param>
/// <param name="UniqueCount">Number of distinct rows (each row is one printing).</param>
/// <param name="FoilCount">Sum of <see cref="PhysicalMtgCard.Count"/> for rows whose <see cref="PhysicalMtgCard.Finish"/> is Foil or Etched.</param>
/// <param name="TotalValue">Sum of <c>PriceBought × Count</c> across all priced rows, or null if no prices or currencies are mixed.</param>
/// <param name="MostExpensive">The row with the highest <em>unit</em> price (regardless of Count), or null if no prices.</param>
public sealed record CollectionSummary(
	int TotalCount,
	int UniqueCount,
	int FoilCount,
	Money? TotalValue,
	PhysicalMtgCard? MostExpensive)
{
	public static CollectionSummary From(IReadOnlyList<PhysicalMtgCard> cards)
	{
		var priced = cards.Where(c => c.PriceBought is not null).ToList();
		Money? totalValue = null;
		if (priced.Count > 0)
		{
			var currency = priced[0].PriceBought!.Currency;
			// Skip the total when currencies are mixed — adding USD to EUR would be misleading.
			if (priced.All(c => c.PriceBought!.Currency.Equals(currency)))
			{
				var sum = priced.Sum(c => c.PriceBought!.Value * c.Count);
				totalValue = new Money(sum, currency);
			}
		}

		return new CollectionSummary(
			TotalCount: cards.Sum(c => c.Count),
			UniqueCount: cards.Count,
			FoilCount: cards.Where(c => c.Finish is CardFinish.Foil or CardFinish.Etched).Sum(c => c.Count),
			TotalValue: totalValue,
			MostExpensive: priced.OrderByDescending(c => c.PriceBought!.Value).FirstOrDefault());
	}
}
