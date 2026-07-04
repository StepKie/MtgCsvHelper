namespace MtgCsvHelper.Tests;

public class FormatDetectorTests(ITestOutputHelper output) : BaseTest(output)
{
	FormatDetector NewDetector() => new([.. CardMapFactory.SupportedConfigs(_config)]);

	// Canonical cases live in the fixture-driven theories below; inline rows cover only what no fixture can (CARDKINGDOM, quoted headers).
	[Theory]
	[InlineData("quantity,title,edition,foil", "CARDKINGDOM")]
	[InlineData("QUANTITY,\"NAME\",SETCODE,\"SETNAME\",\"COLLECTOR NUMBER\",FINISH,PRICE,RARITY,ID,ACQUIRED DATE,ACQUIRED PRICE,LANG,PRICE SALE,SIGNING,ALTERATION,CONDITION,NOTES,TAGS", "TOPDECKED")]
	[InlineData("\"Count\",\"Tradelist Count\",\"Name\",\"Edition\",\"Condition\",\"Language\",\"Foil\",\"Tags\",\"Last Modified\",\"Collector Number\",\"Alter\",\"Proxy\",\"Purchase Price\"", "MOXFIELD")]
	public void DetectsKnownFormatsFromTheirHeaders(string headerLine, string expectedFormat)
	{
		NewDetector().Detect(headerLine).Should().Be(expectedFormat);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("foo,bar,baz")]
	[InlineData("name,price")]  // 0 hits — too generic
	public void ReturnsNullForUnrecognizableHeaders(string headerLine)
	{
		NewDetector().Detect(headerLine).Should().BeNull();
	}

	[Fact]
	public void StripsSepMarker_HandledByCaller_NotDetector()
	{
		// The detector itself is dumb about "sep=,"; callers strip the marker first.
		// Verifying behavior on raw header (without the marker) here.
		NewDetector().Detect("Quantity,Card Name,Set Code,Set Name,Card Number").Should().Be("DRAGONSHIELD");
	}

	// Real fixture files carry the quoting/casing quirks hand-typed headers miss (Topdecked and Moxfield quote some columns).
	[Theory]
	[InlineData("dragonshield-collection.csv", "DRAGONSHIELD")]
	[InlineData("moxfield-haves.csv", "MOXFIELD")]
	[InlineData("moxfield_pauper_cube.csv", "MOXFIELD")]
	[InlineData("manabox-collection.csv", "MANABOX")]
	[InlineData("topdecked-collection.csv", "TOPDECKED")]
	[InlineData("mtggoldfish-collection.csv", "MTGGOLDFISH")]
	[InlineData("mtggoldfish-from-mtgarena.csv", "MTGGOLDFISH")]
	[InlineData("tcgplayer-collection.csv", "TCGPLAYER")]
	[InlineData("tcgplayer-collection-old.csv", "TCGPLAYER")]
	[InlineData("mtgo-collection.csv", "MTGO")]
	public void DetectsRealFixtureFiles(string fixtureName, string expectedFormat)
	{
		var path = Path.Combine("Resources", "SampleCsvs", "Collection", fixtureName);
		using var stream = File.OpenRead(path);
		NewDetector().Detect(stream).Should().Be(expectedFormat);
	}

	public static TheoryData<string> RealExportFixtures() =>
		new(Directory.EnumerateFiles(Path.Combine("Resources", "SampleCsvs", "Tests"), "*-real-export.csv").Select(Path.GetFileName)!);

	// Real exports carry each site's exact quoting, casing, and delimiter; the expected format derives from the filename.
	[Theory]
	[MemberData(nameof(RealExportFixtures))]
	public void DetectsRealExportFixtures(string fixtureName)
	{
		var path = Path.Combine("Resources", "SampleCsvs", "Tests", fixtureName);
		using var stream = File.OpenRead(path);
		NewDetector().Detect(stream).Should().Be(CsvFixture.FormatFromFilename(fixtureName));
	}

	// DetectAsync goes through ReadHeaderAsync, a separate code path from ReadHeader — exercise
	// it on the fixture with the trickiest header (quoted "sep=," marker + commas in the line).
	[Fact]
	public async Task DetectAsync_AgreesWithSyncOverload()
	{
		var path = Path.Combine("Resources", "SampleCsvs", "Collection", "dragonshield-collection.csv");
		await using var stream = File.OpenRead(path);
		(await NewDetector().DetectAsync(stream)).Should().Be("DRAGONSHIELD");
	}
}
