namespace MtgCsvHelper.Tests;

public class DeckFormatTests(ITestOutputHelper output) : BaseTest(output)
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
		var classMap = new CardMapFactory(_config).GenerateClassMap(deckFormatName);
		classMap.Should().NotBeNull();
	}
}
