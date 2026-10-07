using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

/// <summary>
/// SORTED specifics beyond the generic real-export theories: its two-letter language codes (incl. the
/// non-Scryfall <c>jp</c>/<c>cn</c>/<c>tw</c>), Deck rows, the guild-kit codes inherited from DragonShield,
/// and the write shapes Sorted's importer requires (which DragonShield shares via <c>DragonShieldMap</c>).
/// </summary>
[Collection(CatalogCollection.Name)]
public class SortedTests(CatalogFixture fixture) : ApiBaseTest(fixture)
{
	const string SamplePath = "Resources/SampleCsvs/Tests/sorted-real-export.csv";

	async Task<ParseResult> ParseSample() =>
		await new MtgCardCsvHandler(_catalog, _resolver, _config, "SORTED").ParseCollectionCsvAsync(SamplePath, TestContext.Current.CancellationToken);

	[Fact]
	public async Task LanguageCodes_MapToScryfallCodes()
	{
		var result = await ParseSample();

		var boltLanguages = result.Collection.Cards
			.Where(c => c.Folder == "Imported" && c.Printing.Name == "Lightning Bolt")
			.Select(c => c.Language);

		boltLanguages.Should().BeEquivalentTo(["en", "es", "fr", "de", "it", "pt", "ja", "ru", "zhs", "zht"]);
	}

	[Fact]
	public async Task DeckRows_ImportWithListNameAsFolder()
	{
		var result = await ParseSample();

		var deckCard = result.Collection.Cards.Should().ContainSingle(c => c.Printing.Name == "Sunken Hollow").Which;

		deckCard.Folder.Should().Be("Test");
		deckCard.Printing.Set.Should().Be("BFZ");
		deckCard.Language.Should().BeNull("Sorted leaves Language blank on Deck rows");
	}

	[Fact]
	public async Task GuildKitCode_CollapsesToCanonicalSet()
	{
		var result = await ParseSample();

		result.Collection.Cards.Should().ContainSingle(c => c.Printing.Name == "Isperia, Supreme Judge")
			.Which.Printing.Set.Should().Be("GK2");
	}

	static PhysicalMtgCard Card(string name, string set, string collectorNumber, string setName) => new()
	{
		Count = 1,
		Condition = CardCondition.NearMint,
		Finish = CardFinish.Normal,
		Language = "en",
		Printing = new Card { Name = name, Set = set, SetName = setName, CollectorNumber = collectorNumber },
	};

	string Write(string format, PhysicalMtgCard card) => CsvFixture.WriteToString(new MtgCardCsvHandler(_catalog, _resolver, _config, format), [card]);

	[Theory]
	[InlineData("SORTED")]
	[InlineData("DRAGONSHIELD")]
	public void AdventureCard_KeepsFullName(string format)
	{
		var csv = Write(format, Card("Brazen Borrower // Petty Theft", "ELD", "39", "Throne of Eldraine"));

		csv.Should().Contain(",Brazen Borrower // Petty Theft,", "Sorted rejects an adventure card's front-face name");
	}

	[Theory]
	[InlineData("SORTED")]
	[InlineData("DRAGONSHIELD")]
	public void TheListCard_IsWrittenAsOriginalPrinting(string format)
	{
		var csv = Write(format, Card("Demonic Tutor", "PLST", "DDC-49", "The List"));

		csv.Should().Contain(",Demonic Tutor,DDC,Duel Decks: Divine vs. Demonic,49,", "Sorted rejects The List collector numbers");
	}

	[Fact]
	public void TheListCard_WithoutMatchingOriginal_StaysOnTheList()
	{
		var csv = Write("SORTED", Card("Lightning Bolt", "PLST", "DDC-49", "The List"));

		csv.Should().Contain(",Lightning Bolt,PLST,The List,DDC-49,");
	}
}
