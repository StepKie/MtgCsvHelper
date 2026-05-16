using System.Text;
using CsvHelper;

namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class FaultToleranceTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	const string MoxHeader = "Count,Name,Edition,Collector Number,Foil,Condition,Language,Purchase Price";

	static MemoryStream CsvStream(string csv) => new(Encoding.UTF8.GetBytes(csv));
	MtgCardCsvHandler Handler(string format = "MOXFIELD") => new(_catalog, _resolver, _config, format);

	[Fact]
	public void UnknownSetCode_RaisesWarning_CardStillImported()
	{
		// "FAKESET" is not a real Scryfall set code — post-load enrichment will fail to backfill the set name.
		var csv = MoxHeader + "\n"
			+ "1,Lightning Bolt,FAKESET,149,,Near Mint,English,\n";

		var result = Handler().ParseCollectionCsv(CsvStream(csv));

		result.Collection.Cards.Should().HaveCount(1);
		result.ErrorCount.Should().Be(0);
		result.WarningCount.Should().Be(1);
		result.Issues[0].Severity.Should().Be(IssueSeverity.Warning);
		result.Issues[0].Reason.Should().Contain("FAKESET");
	}

	[Fact]
	public void HeaderMismatch_ThrowsHeaderValidationException_ListingMissingHeaders()
	{
		var csv = "wrong,headers,here\n1,2,3\n";

		var act = () => Handler().ParseCollectionCsv(CsvStream(csv));

		var ex = act.Should().Throw<HeaderValidationException>().Which;
		var missing = ex.InvalidHeaders.SelectMany(h => h.Names).ToList();
		missing.Should().Contain("Name").And.Contain("Count");
	}

	[Fact]
	public void BlankAndDelimiterOnlyRows_SkippedSilently()
	{
		var csv = MoxHeader + "\n"
			+ "2,Lightning Bolt,M11,149,,Near Mint,English,\n"
			+ "\n"                             // truly blank line
			+ ",,,,,,,\n"                      // delimiter-only row (should also be skipped)
			+ "1,Counterspell,MMQ,69,,Near Mint,English,\n";

		var result = Handler().ParseCollectionCsv(CsvStream(csv));

		result.Collection.Cards.Should().HaveCount(2);
		result.Issues.Should().BeEmpty();
	}
}
