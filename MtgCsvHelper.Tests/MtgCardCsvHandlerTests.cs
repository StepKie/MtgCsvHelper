using Microsoft.Extensions.Configuration;
using ScryfallApi.Client.Models;
using Serilog;

namespace MtgCsvHelper.Tests;

public class MtgCardCsvHandlerTests(ITestOutputHelper output) : BaseTest(output)
{
	public const string SAMPLES_FOLDER = "Resources/SampleCsvs/Samples";
	public const string COLLECTIONS_FOLDER = "Resources/SampleCsvs/Collection";

	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	[InlineData("MANABOX")]
	[InlineData("TOPDECKED")]
	[InlineData("DECKBOX")]
	public void WriteReadSampleCycleTest(string deckFormatName)
	{
		// Arrange
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);
		IList<PhysicalMtgCard> originalCards = GetReferenceCards(handler.Format.Currency);

		// Act
		string fileName = $"unittest-{deckFormatName}.csv";
		handler.WriteCollectionCsv(originalCards, fileName);
		List<PhysicalMtgCard> parsedCards = handler.ParseCollectionCsv(fileName).Cards;

		// Assert
		parsedCards.Should().BeEquivalentTo(originalCards);
	}

	[Theory]
	[InlineData($"{SAMPLES_FOLDER}/dragonshield-sample.csv", "DRAGONSHIELD")]
	[InlineData($"{SAMPLES_FOLDER}/moxfield-sample.csv", "MOXFIELD")]
	[InlineData($"{SAMPLES_FOLDER}/manabox-sample.csv", "MANABOX")]
	[InlineData($"{SAMPLES_FOLDER}/topdecked-sample.csv", "TOPDECKED")]
	[InlineData($"{SAMPLES_FOLDER}/deckbox-sample.csv", "DECKBOX")]
	public void ParseSampleCsv_WithValidInput_ParsesCards(string csvFilePath, string deckFormatName)
	{
		// Arrange
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);
		IList<PhysicalMtgCard> expectedCards = GetReferenceCards(handler.Format.Currency);

		// Act
		IList<PhysicalMtgCard> cards = handler.ParseCollectionCsv(csvFilePath).Cards;

		// Assert
		cards.Should().BeEquivalentTo(expectedCards);
	}

	[Theory]
	[InlineData($"{COLLECTIONS_FOLDER}/dragonshield-collection.csv", "DRAGONSHIELD", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/moxfield-collection.csv", "MOXFIELD", "DRAGONSHIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/topdecked-collection.csv", "TOPDECKED", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/manabox-collection.csv", "MANABOX", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/manabox-collection.csv", "MANABOX", "CARDKINGDOM")]
	public async Task ConvertCollectionCsvTest(string csvFilePath, string deckFormatIn, string deckFormatOut)
	{
		// Arrange
		MtgCardCsvHandler handlerIn = CreateHandler(deckFormatIn);
		MtgCardCsvHandler handlerOut = CreateHandler(deckFormatOut);

		var collection = handlerIn.ParseCollectionCsv(csvFilePath);
		var cards = collection.Cards;
		handlerOut.WriteCollectionCsv(cards);

		Log.Information(collection.GenerateSummary());
		cards.Should().HaveCountGreaterThan(1000);
	}

	MtgCardCsvHandler CreateHandler(string deckFormatName)
	{
		IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build();
		DeckFormat format = new(configuration, deckFormatName);

		return new MtgCardCsvHandler(_api, format);
	}

	static List<PhysicalMtgCard> GetReferenceCards(Currency currency)
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
			PriceBought = new Money(0.20m, currency),
		};

		// There is no test for excellent, since some sites only have six conditions (e.g. Moxfield)
		var card3 = card1 with { Condition = CardCondition.GOOD };
		var card4 = card1 with { Condition = CardCondition.LIGHTLY_PLAYED };
		var card5 = card1 with { Condition = CardCondition.PLAYED };
		var card6 = card1 with { Condition = CardCondition.POOR };

		return [card1, card2, card3, card4, card5, card6];
	}
}
