using System.IO.Compression;
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
	public void UnknownSetCode_WithValidName_RewrittenByName()
	{
		// "FAKESET" doesn't resolve by coordinate; the valid name "Lightning Bolt" rewrites it to a real printing (Warning), not dropped.
		var csv = MoxHeader + "\n"
			+ "1,Lightning Bolt,FAKESET,149,,Near Mint,English,\n";

		var result = Handler().ParseCollectionCsv(CsvStream(csv));

		result.ErrorCount.Should().Be(0);
		var printing = result.Collection.Cards.Should().ContainSingle().Which.Printing;
		printing.Name.Should().Be("Lightning Bolt");
		printing.Set.Should().NotBe("FAKESET", because: "the stale set code is rewritten to a real printing");
		result.Issues.Should().Contain(i => i.Severity == IssueSeverity.Warning);
		// RawContent must be threaded from the parse loop through the issues so the UI can show the row.
		result.Issues.Should().AllSatisfy(i => i.RawContent.Should().Contain("Lightning Bolt").And.Contain("FAKESET"));
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
	public void NameDoesNotMatchPrinting_RaisesError_RowDropped()
	{
		// Lightning Bolt at M11 #149 is real, but the name is wrong.
		var csv = MoxHeader + "\n"
			+ "1,Fake Card Name,M11,149,,Near Mint,English,\n";

		var result = Handler().ParseCollectionCsv(CsvStream(csv));

		result.Collection.Cards.Should().BeEmpty();
		result.ErrorCount.Should().Be(1);
		result.Issues[0].Reason.Should().Contain("Fake Card Name").And.Contain("M11").And.Contain("#149");
		result.Issues[0].RawContent.Should().Contain("Fake Card Name");
	}

	[Fact]
	public void FoilOnNonFoilPrinting_RaisesError_RowDropped()
	{
		// Lim-Dûl's Vault from Alliances (ALL #107) is a 1996 pre-foil-era printing;
		// the catalog's Finishes list is ["nonfoil"] only.
		var csv = MoxHeader + "\n"
			+ "1,Lim-Dûl's Vault,ALL,107,foil,Near Mint,English,\n";

		var result = Handler().ParseCollectionCsv(CsvStream(csv));

		result.Collection.Cards.Should().BeEmpty();
		result.ErrorCount.Should().Be(1);
		result.Issues[0].Reason.Should().Contain("foil");
		result.Issues[0].RawContent.Should().Contain("Lim-Dûl's Vault");
	}

	[Fact]
	public void SetAndCollectorNotInCatalog_WithValidName_RewrittenByName()
	{
		// M11 #9999 doesn't exist; the valid name "Lightning Bolt" rewrites it to a real printing (Warning), not dropped.
		var csv = MoxHeader + "\n"
			+ "1,Lightning Bolt,M11,9999,,Near Mint,English,\n";

		var result = Handler().ParseCollectionCsv(CsvStream(csv));

		result.ErrorCount.Should().Be(0);
		result.Collection.Cards.Should().ContainSingle().Which.Printing.Name.Should().Be("Lightning Bolt");
		var warning = result.Issues.Should().ContainSingle(i => i.Severity == IssueSeverity.Warning).Which;
		warning.Reason.Should().Contain("No printing").And.Contain("M11").And.Contain("9999");
		warning.RawContent.Should().Contain("9999");
	}

	[Fact]
	public void NonPositiveCount_RaisesError_RowDropped_WithRawContent()
	{
		// Guards the parse-time RawContent capture (not the enricher pipeline). The Count
		// validation runs inside the GetRecord try-block in MtgCardCsvHandler before the row
		// reaches any enricher; if csv.Parser.RawRecord isn't captured there too the field is
		// null on the emitted issue.
		var csv = MoxHeader + "\n"
			+ "0,Lightning Bolt,M11,149,,Near Mint,English,\n";

		var result = Handler().ParseCollectionCsv(CsvStream(csv));

		result.Collection.Cards.Should().BeEmpty();
		result.ErrorCount.Should().Be(1);
		result.Issues[0].Reason.Should().Contain("Count");
		result.Issues[0].RawContent.Should().Contain("Lightning Bolt").And.StartWith("0,");
	}

	[Fact]
	public void DragonShieldGuildKitCode_ResolvesToScryfallSet()
	{
		var csv = "\"sep=,\"\n"
			+ "Folder Name,Quantity,Trade Quantity,Card Name,Set Code,Set Name,Card Number,Condition,Printing,Language,Price Bought,Date Bought\n"
			+ "Test,1,0,Azorius Herald,GK2_AZORIU,Guild Kit: Azorius,2,NearMint,Normal,English,0.00,2026-05-15\n";

		var result = Handler("DRAGONSHIELD").ParseCollectionCsv(CsvStream(csv));

		result.Issues.Should().NotContain(i => i.Severity == IssueSeverity.Error);
		result.Collection.Cards.Should().ContainSingle()
			.Which.Printing.Set.Should().Be("GK2");
	}

	[Fact]
	public void UnrecognizedCondition_RaisesError_RowDropped()
	{
		// "Pristine" is not in any format's condition vocabulary.
		var csv = MoxHeader + "\n"
			+ "1,Lightning Bolt,M11,149,,Pristine,English,\n";

		var result = Handler().ParseCollectionCsv(CsvStream(csv));

		result.Collection.Cards.Should().BeEmpty();
		result.ErrorCount.Should().Be(1);
		result.Issues[0].Reason.Should().Contain("Pristine").And.Contain("Condition");
	}

	[Fact]
	public void NonSeekableStream_ThrowsArgumentExceptionEarly()
	{
		// GZipStream is the canonical non-seekable stream; the guard must fire before any read.
		using var nonSeekable = new GZipStream(new MemoryStream(), CompressionMode.Decompress);

		var act = () => Handler().ParseCollectionCsv(nonSeekable);

		act.Should().Throw<ArgumentException>().WithParameterName("csvStream").WithMessage("*seekable*");
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
