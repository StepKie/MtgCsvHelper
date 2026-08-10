using System.Text;

namespace MtgCsvHelper.Models;

public record Collection
{
	public required string Name { get; init; }
	public List<PhysicalMtgCard> Cards { get; init; } = [];

	public string GenerateSummary()
	{
		var summary = CollectionSummary.From(Cards);
		StringBuilder sb = new();
		sb.AppendLine(Name);
		sb.AppendLine("-------------------");
		sb.AppendLine($"Total cards: {summary.TotalCount}");
		sb.AppendLine($"Total unique cards: {summary.UniqueCount}");

		if (summary.MostExpensive is { PriceBought: not null } mostExpensive)
		{
			sb.AppendLine($"Most expensive card: {mostExpensive.Printing.Name} ({mostExpensive.PriceBought.Print()})");
		}

		var rarityCounts = Cards
			.Where(c => c.Rarity != CardRarity.Unknown)
			.GroupBy(c => c.Rarity, (rarity, group) => (Rarity: rarity, Amount: group.Sum(c => c.Count)))
			.OrderBy(rc => rc.Rarity);

		foreach (var (rarity, amount) in rarityCounts)
		{
			sb.AppendLine($"{rarity}: {amount}");
		}

		sb.AppendLine("-------------------");

		return sb.ToString();
	}
}
