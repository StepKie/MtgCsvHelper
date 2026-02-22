namespace MtgCsvHelper.Tests;

[Collection(MtgApiCollection.Name)]
public class DeckFormatTests(MtgApiFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	[InlineData("MANABOX")]
	[InlineData("TOPDECKED")]
	[InlineData("DECKBOX")]
	//[InlineData("CARDKINGDOM")]
	public void SupportedTest(string deckFormatName)
	{
		var classMap = new CardMapFactory(_config).GenerateClassMap(deckFormatName, _api);
		classMap.Should().NotBeNull();
	}
}
