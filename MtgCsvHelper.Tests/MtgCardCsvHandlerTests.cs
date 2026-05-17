using ScryfallApi.Client.Models;
using Serilog;

namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class MtgCardCsvHandlerTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	public const string TESTS_FOLDER = "Resources/SampleCsvs/Tests";
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
		IList<PhysicalMtgCard> originalCards = GetReferenceCards(Currency.FromString(currency));

		// Act
		string fileName = $"unittest-{deckFormatName}.csv";
		handler.WriteCollectionCsv(originalCards, fileName);
		List<PhysicalMtgCard> parsedCards = handler.ParseCollectionCsv(fileName).Collection.Cards;

		// Assert
		parsedCards.Should().BeEquivalentTo(originalCards);
	}

	[Theory]
	[InlineData($"{TESTS_FOLDER}/dragonshield-field-fidelity.csv", "DRAGONSHIELD", "USD")]
	[InlineData($"{TESTS_FOLDER}/moxfield-field-fidelity.csv", "MOXFIELD", "EUR")]
	[InlineData($"{TESTS_FOLDER}/manabox-field-fidelity.csv", "MANABOX", "USD")]
	[InlineData($"{TESTS_FOLDER}/topdecked-field-fidelity.csv", "TOPDECKED", "USD")]
	//[InlineData($"{TESTS_FOLDER}/deckbox-field-fidelity.csv", "DECKBOX", "USD")] // TODO #31 (Deckbox set-name aliases).
	public void ParseSampleCsv_WithValidInput_ParsesCards(string csvFilePath, string deckFormatName, string currency)
	{
		// Arrange
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);
		IList<PhysicalMtgCard> expectedCards = GetReferenceCards(Currency.FromString(currency));

		// Act
		IList<PhysicalMtgCard> cards = handler.ParseCollectionCsv(csvFilePath).Collection.Cards;

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
	[InlineData($"{COLLECTIONS_FOLDER}/tcgplayer-collection.csv", "TCGPLAYER", "MANABOX")]
	public void ConvertCollectionCsvTest(string csvFilePath, string deckFormatIn, string deckFormatOut)
	{
		// Arrange
		MtgCardCsvHandler handlerIn = CreateHandler(deckFormatIn);
		MtgCardCsvHandler handlerOut = CreateHandler(deckFormatOut);

		var result = handlerIn.ParseCollectionCsv(csvFilePath);
		var cards = result.Collection.Cards;
		var resultFileName = $"unittest_{deckFormatIn}-to-{deckFormatOut}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
		handlerOut.WriteCollectionCsv(cards, resultFileName);

		Log.Information(result.Collection.GenerateSummary());
		cards.Should().HaveCountGreaterThan(500);
	}

	[Fact]
	public void Mtgo_AllRowsParse_WithCollectorNumberStrippedAndMtgoCodeAliased()
	{
		// MTGO fixture has 7 rows of old-set printings (Mirage, Visions, Tempest, Exodus).
		// Two MTGO-specific quirks the parser handles: collector numbers in "N/M" form
		// (116/350 → 116) and 2-letter set codes (MI → MIR via the bundled mtgo_code map).
		var handler = CreateHandler("MTGO");
		var result = handler.ParseCollectionCsv($"{COLLECTIONS_FOLDER}/mtgo-collection.csv");

		result.Collection.Cards.Should().HaveCount(7);
		result.ErrorCount.Should().Be(0,
			$"all 7 rows are real printings with valid (Set, Collector#) once aliased. Issues: {string.Join("; ", result.Issues.Select(i => i.Reason))}");

		// "116/350" → "116" stripping verification.
		result.Collection.Cards.Select(c => c.Printing.CollectorNumber)
			.Should().AllSatisfy(n => n.Should().NotContain("/"));

		// MTGO 2-letter codes (MI, VI, TE, EX) must be rewritten to canonical 3-letter Scryfall
		// codes (MIR, VIS, TMP, EXO) so MTGO → other-format conversions emit the right code.
		result.Collection.Cards.Should().AllSatisfy(c =>
			c.Printing.Set.Should().HaveLength(3, "MTGO 2-letter codes should be resolved to canonical Scryfall codes"));
	}

	[Fact]
	public void Mtgo_ConvertToMoxfield_EmitsCanonicalSetCodesInOutput()
	{
		// End-to-end through the WRITE path: parse MTGO, write to Moxfield, then inspect the
		// emitted CSV. Catches any regression that bypasses the in-memory canonicalization but
		// still produces a non-canonical code in the output column.
		var reader = CreateHandler("MTGO");
		var writer = CreateHandler("MOXFIELD");

		var cards = reader.ParseCollectionCsv($"{COLLECTIONS_FOLDER}/mtgo-collection.csv").Collection.Cards;
		using var output = new MemoryStream();
		writer.WriteCollectionCsv(cards, output);
		var csv = System.Text.Encoding.UTF8.GetString(output.ToArray());

		// Each canonical 3-letter code from the 7 fixture rows must appear in the output.
		foreach (var canonical in new[] { "MIR", "VIS", "TMP", "EXO" })
		{
			csv.Should().Contain(canonical, $"row(s) of {canonical} should round-trip through the write path");
		}
		// And no MTGO 2-letter code should leak through.
		foreach (var mtgo in new[] { ",MI,", ",VI,", ",TE,", ",EX," })
		{
			csv.Should().NotContain(mtgo, $"MTGO 2-letter code {mtgo.Trim(',')} must be canonicalized before write");
		}
	}

	MtgCardCsvHandler CreateHandler(string deckFormatName) => new(_catalog, _resolver, _config, deckFormatName);

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

        var card2 = card1 with { Language = "zht" };

        // Card3 verifies Count, Foil, Language, PriceBought
        var card3 = card1 with
		{
			Count = 2,
			Condition = CardCondition.NEAR_MINT,
			Foil = true,
			Language = "de",
			PriceBought = new Money(0.20m, currency),
		};

        // There is no test for excellent, since some sites only have six conditions (e.g. Moxfield)
        var card4 = card1 with { Condition = CardCondition.GOOD };
		var card5 = card1 with { Condition = CardCondition.LIGHTLY_PLAYED };
		var card6 = card1 with { Condition = CardCondition.PLAYED };
		var card7 = card1 with { Condition = CardCondition.POOR };

		var card8 = new PhysicalMtgCard
		{
			Count = 1,
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

		var card9 = new PhysicalMtgCard
		{
			Count = 1,
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

		return [card1, card2, card3, card4, card5, card6, card7, card8, card9];
	}
}
