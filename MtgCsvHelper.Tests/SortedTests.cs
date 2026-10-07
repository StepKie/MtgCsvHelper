namespace MtgCsvHelper.Tests;

/// <summary>
/// SORTED specifics beyond the generic real-export theories: its two-letter language codes (incl. the
/// non-Scryfall <c>jp</c>/<c>cn</c>/<c>tw</c>), Deck rows, and the guild-kit codes inherited from DragonShield.
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
}
