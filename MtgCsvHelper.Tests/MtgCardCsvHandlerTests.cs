using Microsoft.Extensions.Configuration;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

public class MtgCardCsvHandlerTests(ITestOutputHelper output) : BaseTest(output)
{
	public const string SAMPLES_FOLDER = "Resources/SampleCsvs/Samples";
	public const string COLLECTIONS_FOLDER = "Resources/SampleCsvs/Collection";

	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	[InlineData("MANABOX")]
	//[InlineData("DECKBOX")]
	public void WriteReadSampleCycleTest(string deckFormatName)
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
	[InlineData($"{SAMPLES_FOLDER}/manabox-sample.csv", "MANABOX")]
	// There is an issue with the price ($) for deckbox which needs to be figured out first
	// Possibly remove since we have different units (euro/usd) for different sites
	//[InlineData($"{SAMPLES_FOLDER}/deckbox-sample.csv", "DECKBOX")]
	public void ParseSampleCsv_WithValidInput_ParsesCards(string csvFilePath, string deckFormatName)
	{
		// Arrange
		IList<PhysicalMtgCard> expectedCards = GetReferenceCards();
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);

		// Act
		IList<PhysicalMtgCard> cards = handler.ParseCollectionCsv(csvFilePath);

		// Assert
		cards.Should().BeEquivalentTo(expectedCards);
	}

	[Theory]
	[InlineData($"{COLLECTIONS_FOLDER}/dragonshield-collection.csv", "DRAGONSHIELD", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/moxfield-collection.csv", "MOXFIELD", "DRAGONSHIELD")]
	public void ParseCollectionCsvTest(string csvFilePath, string deckFormatIn, string deckFormatOut)
	{
		// Arrange
		MtgCardCsvHandler handlerIn = CreateHandler(deckFormatIn);
		MtgCardCsvHandler handlerOut = CreateHandler(deckFormatOut);

		// Act
		IList<PhysicalMtgCard> cards = handlerIn.ParseCollectionCsv(csvFilePath);
		handlerOut.WriteCollectionCsv(cards);
		cards.Should().HaveCountGreaterThan(1000);
	}

	MtgCardCsvHandler CreateHandler(string deckFormatName)
	{
		IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build();
		DeckFormat format = new(configuration, deckFormatName);

		return new MtgCardCsvHandler(_api, format);
	}

	static List<PhysicalMtgCard> GetReferenceCards()
	{
		var card1 = new PhysicalMtgCard
		{
			Count = 1,
			Condition = CardCondition.MINT,
			Foil = false,
			Printing = new Card
			{
				Name = "Ambitious Farmhand // Seasoned Cathar",
				CollectorNumber = "2",
				Set = "MID",
				SetName = "Innistrad: Midnight Hunt",
			},
			Language = "en",
		};

		// Card2 verifies Count, Foil, Language, PriceBought
		var card2 = card1 with
		{
			Count = 2,
			Condition = CardCondition.NEAR_MINT,
			Foil = true,
			Language = "de",
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
