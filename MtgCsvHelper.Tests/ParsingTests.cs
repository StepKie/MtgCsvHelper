using Microsoft.Extensions.Configuration;

namespace MtgCsvHelper.Tests;

public class MtgCardCsvHandlerTests
{

	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	[InlineData("DECKBOX")]
	public void WriteReadCycleTest(string deckFormatName)
	{
		// Arrange
		IList<PhysicalMtgCard> originalCards = GetReferenceCards();
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);

		// Act
		string fileName = $"unittest-{deckFormatName}.csv";
		handler.WriteCollectionCsv(originalCards, fileName);
		IList<PhysicalMtgCard> parsedCards = handler.ParseCollectionCsv(fileName);

		// Assert
		parsedCards.First().Printing.Identifier.Should().BeEquivalentTo("MID#2");
		parsedCards.Should().HaveCount(7);
		parsedCards.Should().BeEquivalentTo(originalCards);
	}

	[Theory]
	[InlineData("SampleCsvs/dragonshield-sample.csv", "DRAGONSHIELD")]
	[InlineData("SampleCsvs/moxfield-sample.csv", "MOXFIELD")]
	[InlineData("SampleCsvs/deckbox-sample.csv", "DECKBOX")]
	public void ParseCollectionCsv_WithValidInput_ParsesCards(string csvFilePath, string deckFormatName)
	{
		// Arrange
		IList<PhysicalMtgCard> expectedCards = GetReferenceCards();
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);

		// Act
		IList<PhysicalMtgCard> cards = handler.ParseCollectionCsv(csvFilePath);

		// Assert
		cards.First().Printing.Identifier.Should().BeEquivalentTo("MID#2");
		cards.Should().HaveCount(7);
		cards.Should().BeEquivalentTo(expectedCards);
	}

	[Fact()]
	public void WriteCollectionCsvTest()
	{
		Assert.True(false, "This test needs an implementation");
	}

	MtgCardCsvHandler CreateHandler(string deckFormatName)
	{
		// Read configuration
		IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build();
		DeckFormat format = new(configuration, deckFormatName);

		return new MtgCardCsvHandler(format);
	}

	static List<PhysicalMtgCard> GetReferenceCards()
	{
		var card1 = new PhysicalMtgCard
		{
			Count = 1,
			Condition = CardCondition.MINT,
			Foil = false,
			Printing = new Printing
			{
				Card = new MtgCard { Name = "Ambitious Farmhand // Seasoned Cathar" },
				IdInSet = "2",
				Set = new Set { Code = "MID", FullName = "Innistrad: Midnight Hunt" },
			},
			Language = "English",
			//DateBought = new(year: 2020, month: 1, day: 29),
			//PriceBought = 0.2m,
		};

		var card2 = card1 with { Count = 2, Condition = CardCondition.NEAR_MINT, Foil = true, Language = "German", PriceBought = 0.14m };
		var card3 = card1 with { Condition = CardCondition.EXCELLENT };
		var card4 = card1 with { Condition = CardCondition.GOOD };
		var card5 = card1 with { Condition = CardCondition.LIGHTLY_PLAYED };
		var card6 = card1 with { Condition = CardCondition.PLAYED };
		var card7 = card1 with { Condition = CardCondition.POOR };

		return new[] { card1, card2, card3, card4, card5, card6, card7 }.ToList();
	}
}
