using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

public class CollectionTests
{
	static PhysicalMtgCard CardOf(string name, CardRarity rarity, int count) =>
		new() { Count = count, Printing = new Card { Name = name }, Rarity = rarity };

	[Fact]
	public void GenerateSummary_BreaksDownCountsByRarity()
	{
		var collection = new Collection
		{
			Name = "Test",
			Cards =
			[
				CardOf("Lightning Bolt", CardRarity.Common, 4),
				CardOf("Counterspell", CardRarity.Common, 2),
				CardOf("Wrath of God", CardRarity.Rare, 1),
			],
		};

		var summary = collection.GenerateSummary();

		summary.Should().Contain("Common: 6").And.Contain("Rare: 1");
	}

	[Fact]
	public void GenerateSummary_SkipsCardsWithUnknownRarity()
	{
		var collection = new Collection
		{
			Name = "Test",
			Cards = [CardOf("Mystery Card", CardRarity.Unknown, 3)],
		};

		var summary = collection.GenerateSummary();

		summary.Should().NotContain("Unknown");
	}
}
