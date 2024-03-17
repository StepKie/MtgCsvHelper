using System.Text;

namespace MtgCsvHelper.Models;

public record CardCollectionEntry(PhysicalMtgCard Card, int Amount);

public record Collection
{
	public string? Name { get; init; }
	public List<CardCollectionEntry> Entries { get; init; } = [];
	public IEnumerable<PhysicalMtgCard> Cards => Entries.Select(c => c.Card);

	public string GenerateSummary()
	{
		StringBuilder sb = new();
		int numOfCards = Entries.Sum(c => c.Amount);
		int numOfUniqueCards = Entries.Count;

		var mostExpensive = Cards.OrderByDescending(c => c.PriceBought?.Value).FirstOrDefault();

		sb.AppendLine(Name);
		sb.AppendLine("-------------------");
		sb.AppendLine($"Total cards: {numOfCards}");
		sb.AppendLine($"Total unique cards: {numOfUniqueCards}");

		if (mostExpensive?.PriceBought is not null)
		{
			sb.AppendLine($"Most expensive card: {mostExpensive?.Printing.Name} ({mostExpensive?.PriceBought?.Print()})");
		}

		// TODO Will work when we have queried data from Scryfall regarding the cards
		//Dictionary<string, int> cardsByRarity = Cards.GroupBy(c => c.Printing.Rarity).ToDictionary(g => g.Key, g => g.Sum(c => c.Count));

		//foreach (var (rarity, amount) in cardsByRarity)
		//{
		//	sb.AppendLine($"{rarity}: {amount}");
		//}

		sb.AppendLine("-------------------");

		var summary = sb.ToString();
		return summary;
	}
}

