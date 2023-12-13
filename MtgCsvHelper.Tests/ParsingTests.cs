using Microsoft.Extensions.Configuration;

namespace MtgCsvHelper.Tests;

public class MtgCardCsvHandlerTests
{
	public const string SAMPLES_FOLDER = "Resources/SampleCsvs/Samples";

	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	//[InlineData("DECKBOX")]
	public void WriteReadCycleTest(string deckFormatName)
	{
		// Arrange
		IList<PhysicalMtgCard> originalCards = GetReferenceCards();
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);

		// Act
		string fileName = $"unittest-{deckFormatName}.csv";
		handler.WriteCollectionCsv(originalCards, fileName);
		List<PhysicalMtgCard> parsedCards = handler.ParseCollectionCsv(fileName);

		// Assert
		parsedCards.Should().BeEquivalentTo(originalCards);
	}

	[Theory]
	[InlineData($"{SAMPLES_FOLDER}/dragonshield-sample.csv", "DRAGONSHIELD")]
	[InlineData($"{SAMPLES_FOLDER}/moxfield-sample.csv", "MOXFIELD")]
	// There is an issue with the price ($) for deckbox which needs to be figured out first
	// Possibly remove since we have different units (euro/usd) for different sites
	//[InlineData($"{SAMPLES_FOLDER}/deckbox-sample.csv", "DECKBOX")]
	public void ParseCollectionCsv_WithValidInput_ParsesCards(string csvFilePath, string deckFormatName)
	{
		// Arrange
		IList<PhysicalMtgCard> expectedCards = GetReferenceCards();
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);

		// Act
		IList<PhysicalMtgCard> cards = handler.ParseCollectionCsv(csvFilePath);

		// Assert
		cards.First().Printing.Identifier.Should().BeEquivalentTo("MID#2");
		cards.Should().BeEquivalentTo(expectedCards);
	}

	[Fact(Skip = "TODO")]
	public void WriteCollectionCsvTest()
	{
		Assert.Fail("This test needs an implementation");
	}

	static MtgCardCsvHandler CreateHandler(string deckFormatName)
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
		};

		// Card2 verifies Count, Foil, Language, PriceBought
		var card2 = card1 with
		{
			Count = 2,
			Condition = CardCondition.NEAR_MINT,
			Foil = true,
			Language = "German",
			PriceBought = 0.20m,
		};

		// There is no test for excellent, since some sites only have six conditions (e.g. Moxfield)
		var card3 = card1 with { Condition = CardCondition.GOOD };
		var card4 = card1 with { Condition = CardCondition.LIGHTLY_PLAYED };
		var card5 = card1 with { Condition = CardCondition.PLAYED };
		var card6 = card1 with { Condition = CardCondition.POOR };

		return [card1, card2, card3, card4, card5, card6];
	}
}
