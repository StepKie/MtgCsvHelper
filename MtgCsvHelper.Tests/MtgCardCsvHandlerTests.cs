using Microsoft.Extensions.Configuration;
using ScryfallApi.Client.Models;
using Serilog;

namespace MtgCsvHelper.Tests;

public class MtgCardCsvHandlerTests(ITestOutputHelper output) : BaseTest(output)
{
	public const string SAMPLES_FOLDER = "Resources/SampleCsvs/Samples";
	public const string COLLECTIONS_FOLDER = "Resources/SampleCsvs/Collection";

	[Theory]
	[InlineData("DRAGONSHIELD", "USD")]
	[InlineData("MOXFIELD", "EUR")]
	[InlineData("MANABOX", "USD")]
	[InlineData("TOPDECKED", "USD")]
	[InlineData("DECKBOX", "USD")]
	public void WriteReadSampleCycleTest(string deckFormatName, string currency)
	{
		// Arrange
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);
		var originalCards = GetReferenceCards(Currency.FromString(currency));

		// Act
		string fileName = $"unittest-{deckFormatName}.csv";
		handler.WriteCollectionCsv(originalCards, fileName);
		var parsedCards = handler.ParseCollectionCsv(fileName);

		// Assert
		parsedCards.Should().BeEquivalentTo(originalCards);
	}

	[Theory]
	[InlineData($"{SAMPLES_FOLDER}/dragonshield-sample.csv", "DRAGONSHIELD", "USD")]
	[InlineData($"{SAMPLES_FOLDER}/moxfield-sample.csv", "MOXFIELD", "EUR")]
	[InlineData($"{SAMPLES_FOLDER}/manabox-sample.csv", "MANABOX", "USD")]
	[InlineData($"{SAMPLES_FOLDER}/topdecked-sample.csv", "TOPDECKED", "USD")]
	//[InlineData($"{SAMPLES_FOLDER}/deckbox-sample.csv", "DECKBOX", "USD")] // TODO special Deckbox set names.
	public void ParseSampleCsv_WithValidInput_ParsesCards(string csvFilePath, string deckFormatName, string currency)
	{
		// Arrange
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);
		Collection expectedCards = GetReferenceCards(Currency.FromString(currency));

		// Act
		Collection cards = handler.ParseCollectionCsv(csvFilePath);

		// Assert
		cards.Should().BeEquivalentTo(expectedCards);
	}

	[Theory]
	[InlineData($"{COLLECTIONS_FOLDER}/dragonshield-collection.csv", "DRAGONSHIELD", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/moxfield-collection.csv", "MOXFIELD", "DRAGONSHIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/moxfield-collection.csv", "MOXFIELD", "TOPDECKED")]
	[InlineData($"{COLLECTIONS_FOLDER}/topdecked-collection.csv", "TOPDECKED", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/manabox-collection.csv", "MANABOX", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/manabox-collection.csv", "MANABOX", "CARDKINGDOM")]
	[InlineData($"{COLLECTIONS_FOLDER}/mtggoldfish-collection.csv", "MTGGOLDFISH", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/mtggoldfish-from-mtgarena.csv", "MTGGOLDFISH", "MOXFIELD")]
	public void ConvertCollectionCsvTest(string csvFilePath, string deckFormatIn, string deckFormatOut)
	{
		// Arrange
		MtgCardCsvHandler handlerIn = CreateHandler(deckFormatIn);
		MtgCardCsvHandler handlerOut = CreateHandler(deckFormatOut);

		var collection = handlerIn.ParseCollectionCsv(csvFilePath);
		var resultFileName = $"unittest_{deckFormatIn}-to-{deckFormatOut}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
		handlerOut.WriteCollectionCsv(collection, resultFileName);

		Log.Information(collection.GenerateSummary());
		collection.Cards.Should().HaveCountGreaterThan(500);
	}

	MtgCardCsvHandler CreateHandler(string deckFormatName)
	{
		IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build();

		return new MtgCardCsvHandler(_api, configuration, deckFormatName);
	}

	static Collection GetReferenceCards(Currency currency)
	{
		var card1 = new PhysicalMtgCard
		{
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
			Condition = CardCondition.NEAR_MINT,
			Foil = true,
			Language = "de",
			PriceBought = new Money(0.20m, currency),
		};

		var card7 = new PhysicalMtgCard
		{
			Condition = CardCondition.NEAR_MINT,
			Foil = false,
			Printing = new Card
			{
				Name = "Clue",
				CollectorNumber = "14",
				Set = "TMH2",
				SetName = "Modern Horizons 2 Tokens",
			},
			Language = "en",
			PriceBought = new Money(0.15m, currency),
		};

		var card8 = new PhysicalMtgCard
		{
			Condition = CardCondition.NEAR_MINT,
			Foil = false,
			Printing = new Card
			{
				Name = "Food",
				CollectorNumber = "10",
				Set = "TLTR",
				SetName = "Tales of Middle-earth Tokens",
			},
			Language = "en",
			PriceBought = new Money(0.11m, currency),
		};

		return new Collection
		{
			Entries = [
				new(card1, 1),
				new(card2, 2),
				new(card1 with { Condition = CardCondition.GOOD }, 1),
				new(card1 with { Condition = CardCondition.LIGHTLY_PLAYED }, 1),
				new(card1 with { Condition = CardCondition.PLAYED }, 1),
				new(card1 with { Condition = CardCondition.POOR }, 1),
				// There is no test for excellent, since some sites only have six conditions (e.g. Moxfield)
				new(card7, 1),
				new(card8, 1),
				],
		};
	}
}
