using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class TcgplayerWriteTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	static PhysicalMtgCard Card(string name, string set, string collectorNumber) => new()
	{
		Count = 1,
		Condition = CardCondition.NearMint,
		Finish = CardFinish.Normal,
		Language = "en",
		Printing = new Card { Name = name, Set = set, SetName = "", CollectorNumber = collectorNumber },
	};

	string WriteTcgplayer(params PhysicalMtgCard[] cards) =>
		CsvFixture.WriteToString(new MtgCardCsvHandler(_catalog, _resolver, _config, "TCGPLAYER"), cards);

	[Fact]
	public void BorderlessPrinting_GetsVariantSuffixInNameColumn_SimpleNameStaysPlain()
	{
		// LTR #433 is the borderless Orcish Bowmasters; #103 is the normal printing.
		var csv = WriteTcgplayer(Card("Orcish Bowmasters", "LTR", "433"));

		// Both headers are emitted as standalone columns (not just "Name" as a substring of "Simple Name").
		var headers = csv.Split('\n').First(l => l.Contains("Simple Name")).Split(',').Select(h => h.Trim().Trim('"')).ToList();
		headers.Should().Contain("Name").And.Contain("Simple Name");
		csv.Should().Contain("Orcish Bowmasters (Borderless)");

		// Simple Name stays plain: strip the decorated occurrence and the bare name must still be present.
		var dataLine = csv.Split('\n').First(l => l.Contains("Orcish Bowmasters"));
		dataLine.Should().Contain("Orcish Bowmasters (Borderless)");
		dataLine.Replace("Orcish Bowmasters (Borderless)", "").Should().Contain("Orcish Bowmasters");
	}

	[Fact]
	public void NormalPrinting_NameMatchesSimpleName()
	{
		var csv = WriteTcgplayer(Card("Orcish Bowmasters", "LTR", "103"));

		csv.Should().NotContain("(Borderless)");
		csv.Should().Contain("Orcish Bowmasters");
	}

	[Fact]
	public void UnknownPrinting_FallsBackToPlainName()
	{
		// Collector number "999" doesn't exist in the catalog — the map must fall back to the plain name, not throw.
		var csv = WriteTcgplayer(Card("Orcish Bowmasters", "LTR", "999"));

		csv.Should().Contain("Orcish Bowmasters");
		csv.Should().NotContain("(Borderless)").And.NotContain("(Showcase)").And.NotContain("(Extended Art)");
	}
}
