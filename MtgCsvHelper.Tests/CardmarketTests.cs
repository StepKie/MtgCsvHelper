using System.Text;
using CsvHelper;

namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class CardmarketTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	const string SamplePath = "Resources/SampleCsvs/Tests/cardmarket-field-fidelity.csv";

	static MemoryStream CsvStream(string csv) => new(Encoding.UTF8.GetBytes(csv));

	MtgCardCsvHandler Handler() => new(_catalog, _resolver, _config, "CARDMARKET");

	[Fact]
	public async Task ParseSample_ResolvesAllFiveCardsFromCatalog()
	{
		var result = await Handler().ParseCollectionCsvAsync(SamplePath);

		result.Collection.Cards.Should().HaveCount(5);
		result.ErrorCount.Should().Be(0);
		result.WarningCount.Should().Be(0);

		var byName = result.Collection.Cards.ToDictionary(c => c.Printing.Name);

		byName.Should().ContainKey("Pillory of the Sleepless");
		byName.Should().ContainKey("Putrid Leech");
		byName.Should().ContainKey("Master's Rebuke");
		byName.Should().ContainKey("Cliffgate");
		byName.Should().ContainKey("Serrated Arrows");

		// Each card got a set populated from Scryfall (was empty in the source CSV).
		result.Collection.Cards.Should().AllSatisfy(c => c.Printing.Set.Should().NotBeNullOrEmpty());
		result.Collection.Cards.Should().AllSatisfy(c => c.Printing.SetName.Should().NotBeNullOrEmpty());
	}

	[Fact]
	public async Task ParseSample_FoilFlagDecodedCorrectly()
	{
		var result = await Handler().ParseCollectionCsvAsync(SamplePath);
		var byName = result.Collection.Cards.ToDictionary(c => c.Printing.Name);

		byName["Master's Rebuke"].Finish.Should().Be(CardFinish.Foil);   // isFoil=1 in fixture
		byName["Cliffgate"].Finish.Should().Be(CardFinish.Foil);         // isFoil=1 in fixture
		byName["Putrid Leech"].Finish.Should().Be(CardFinish.Normal);    // isFoil empty in fixture
	}

	[Fact]
	public async Task ParseSample_ConditionDecodedCorrectly()
	{
		var result = await Handler().ParseCollectionCsvAsync(SamplePath);
		var byName = result.Collection.Cards.ToDictionary(c => c.Printing.Name);

		byName["Pillory of the Sleepless"].Condition.Should().Be(CardCondition.Excellent); // condition=3
		byName["Putrid Leech"].Condition.Should().Be(CardCondition.NearMint);              // condition=2
		byName["Serrated Arrows"].Condition.Should().Be(CardCondition.Excellent);          // condition=3
	}

	[Fact]
	public async Task ParseSample_LanguageDecodedCorrectly()
	{
		var result = await Handler().ParseCollectionCsvAsync(SamplePath);

		// All rows in the fixture have idLanguage=1 → English ("en")
		result.Collection.Cards.Should().AllSatisfy(c => c.Language.Should().Be("en"));
	}

	[Fact]
	public async Task UnknownCardmarketId_RaisesError_DropsCardKeepsOthers()
	{
		// 99999999 is far above any real cardmarket_id; Scryfall returns it as not_found.
		var csv = "idProduct;groupCount;price;idLanguage;condition;isFoil;isSigned;isAltered;isPlayset;isReverseHolo;isFirstEd;isFullArt;isUberRare;isWithDie\n"
			+ "266380;1;0.15;1;2;;;;;;;;;\n"     // valid: Putrid Leech
			+ "99999999;1;0.10;1;2;;;;;;;;;\n";  // invalid: unknown ID

		var result = await Handler().ParseCollectionCsvAsync(CsvStream(csv));

		// The unresolved card is dropped (without name/set we can't write a meaningful row).
		result.Collection.Cards.Should().HaveCount(1);
		result.ErrorCount.Should().Be(1);
		result.WarningCount.Should().Be(0);
		result.Issues[0].Reason.Should().Contain("99999999");
		result.Issues[0].Severity.Should().Be(IssueSeverity.Error);

		// The valid card still resolved.
		result.Collection.Cards.Should().Contain(c => c.Printing.Name == "Putrid Leech");
	}

	[Fact]
	public async Task ParseWrongFormat_AsCardmarket_ThrowsHeaderValidationException()
	{
		// Moxfield-shaped CSV fed to the CARDMARKET handler — the required 'idProduct' / 'groupCount' headers are absent.
		var csv = "Count,Name,Edition,Collector Number,Foil,Condition,Language,Purchase Price\n"
			+ "1,Lightning Bolt,M11,149,,Near Mint,English,\n";

		var act = async () => await Handler().ParseCollectionCsvAsync(CsvStream(csv));

		await act.Should().ThrowAsync<HeaderValidationException>();
	}

	[Fact]
	public void GenerateWriteMap_ForCardmarket_Throws()
	{
		var factory = new CardMapFactory(_config, _catalog);
		var act = () => factory.GenerateWriteMap("CARDMARKET");

		act.Should().Throw<InvalidOperationException>().WithMessage("*read-only*");
	}
}
