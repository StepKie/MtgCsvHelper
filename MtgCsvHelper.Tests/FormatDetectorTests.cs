namespace MtgCsvHelper.Tests;

public class FormatDetectorTests(ITestOutputHelper output) : BaseTest(output)
{
	FormatDetector NewDetector() => new([.. CardMapFactory.From(_config)]);

	[Theory]
	[InlineData("Quantity,Card Name,Set Code,Set Name,Card Number,Printing,Condition,Language,Price Bought,Date Bought", "DRAGONSHIELD")]
	[InlineData("Count,Tradelist Count,Name,Edition,Collector Number,Foil,Condition,Language,Purchase Price", "MOXFIELD")]
	[InlineData("Quantity,Name,Set code,Set name,Collector number,Foil,Condition,Language,Purchase price", "MANABOX")]
	[InlineData("QUANTITY,NAME,SETCODE,SETNAME,COLLECTOR NUMBER,FINISH,CONDITION,LANG,ACQUIRED PRICE", "TOPDECKED")]
	[InlineData("Count,Tradelist Count,Name,Edition Code,Edition,Card Number,Condition,Language,Foil,My Price", "DECKBOX")]
	[InlineData("Quantity,Simple Name,Set Code,Set,Card Number,Printing,Condition,Language", "TCGPLAYER")]
	[InlineData("Card,Set ID,Set Name,Quantity,Foil,Collector Number", "MTGGOLDFISH")]
	[InlineData("idProduct;groupCount;isFoil;condition;idLanguage;price", "CARDMARKET")]
	// CARDKINGDOM uses lowercase headers, distinct from any other format in the configured set.
	// No matching fixture in Resources/SampleCsvs/Collection (the sample file uses Title-Case
	// headers that don't match the configured map — see appsettings.json) — covering with
	// an explicit InlineData until #61 fills in proper per-format fixtures.
	[InlineData("quantity,title,edition,foil", "CARDKINGDOM")]
	// Quoted-header variants — real exports from these sites quote some/all column names.
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

	// Regression: ensure detection works against the real fixture files, including any
	// quoting / casing quirks of each export. Hand-typed header strings in the theory
	// above missed that Topdecked and Moxfield quote some columns — only reading the
	// actual file catches that.
	[Theory]
	[InlineData("dragonshield-collection.csv", "DRAGONSHIELD")]
	[InlineData("moxfield-collection.csv", "MOXFIELD")]
	[InlineData("moxfield-haves.csv", "MOXFIELD")]
	[InlineData("moxfield_pauper_cube.csv", "MOXFIELD")]
	[InlineData("manabox-collection.csv", "MANABOX")]
	[InlineData("topdecked-collection.csv", "TOPDECKED")]
	[InlineData("mtggoldfish-collection.csv", "MTGGOLDFISH")]
	[InlineData("mtggoldfish-from-mtgarena.csv", "MTGGOLDFISH")]
	[InlineData("tcgplayer-collection.csv", "TCGPLAYER")]
	[InlineData("tcgplayer-collection-old.csv", "TCGPLAYER")]
	public void DetectsRealFixtureFiles(string fixtureName, string expectedFormat)
	{
		var path = Path.Combine("Resources", "SampleCsvs", "Collection", fixtureName);
		using var stream = File.OpenRead(path);
		NewDetector().Detect(stream).Should().Be(expectedFormat);
	}
}
