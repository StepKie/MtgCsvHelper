using ScryfallApi.Client.Models;

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
		var classMap = new CardMapFactory(_config, _api).GenerateClassMap(deckFormatName);
		classMap.Should().NotBeNull();
	}

	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	[InlineData("MANABOX")]
	[InlineData("TOPDECKED")]
	[InlineData("DECKBOX")]
	public void StreamRoundTripTest(string deckFormatName)
	{
		var handler = new MtgCardCsvHandler(_api, _config, deckFormatName);
		var original = new List<PhysicalMtgCard>
		{
			new() { Count = 2, Printing = new Card { Name = "Lightning Bolt", Set = "M11", SetName = "Magic 2011", CollectorNumber = "149" } }
		};

		var stream = new MemoryStream();
		handler.WriteCollectionCsv(original, stream);
		stream.Position = 0;

		var parsed = handler.ParseCollectionCsv(stream).Cards;

		parsed.Should().HaveCount(1);
		parsed[0].Printing.Name.Should().Be("Lightning Bolt");
		parsed[0].Count.Should().Be(2);
	}
}
