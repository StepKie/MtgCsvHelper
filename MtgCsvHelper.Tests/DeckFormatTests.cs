using Microsoft.Extensions.Configuration;

namespace MtgCsvHelper.Tests;

public class DeckFormatTests
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
		var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build();
		var format = new DeckFormat(config, deckFormatName);
		Assert.NotNull(format);
	}

	[Fact(Skip = "Implement Me!")]
	public void GenerateClassMapTest()
	{
		throw new NotImplementedException();
	}
}
