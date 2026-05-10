using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

[Collection(MtgApiCollection.Name)]
public class DeckFormatTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	[InlineData("MANABOX")]
	[InlineData("TOPDECKED")]
	[InlineData("DECKBOX")]
	[InlineData("MTGGOLDFISH")]
	[InlineData("TCGPLAYER")]
	public void GenerateReadMap_ForBidirectionalFormats_Succeeds(string deckFormatName)
	{
		var map = new CardMapFactory(_config, _catalog).GenerateReadMap(deckFormatName);
		map.Should().NotBeNull();
	}

	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	[InlineData("MANABOX")]
	[InlineData("TOPDECKED")]
	[InlineData("DECKBOX")]
	[InlineData("MTGGOLDFISH")]
	[InlineData("TCGPLAYER")]
	[InlineData("CARDKINGDOM")]
	public void GenerateWriteMap_ForAllSupportedFormats_Succeeds(string deckFormatName)
	{
		var map = new CardMapFactory(_config, _catalog).GenerateWriteMap(deckFormatName);
		map.Should().NotBeNull();
	}

	[Fact]
	public void GenerateReadMap_ForCardKingdom_Throws()
	{
		var factory = new CardMapFactory(_config, _catalog);
		var act = () => factory.GenerateReadMap("CARDKINGDOM");
		act.Should().Throw<InvalidOperationException>().WithMessage("*write-only*");
	}

	[Theory]
	[InlineData("UNKNOWN_FORMAT")]
	[InlineData("")]
	public void GenerateReadMap_ForUnknownFormat_ThrowsWithLoadedList(string deckFormatName)
	{
		var factory = new CardMapFactory(_config, _catalog);
		var act = () => factory.GenerateReadMap(deckFormatName);
		act.Should().Throw<ArgumentException>()
			.WithMessage("*MOXFIELD*"); // loaded formats appear in the message
	}

	[Fact]
	public void GenerateReadMap_WithNoConfigLoaded_ThrowsHelpfulError()
	{
		// Empty configuration — simulates appsettings.json failing to load.
		var emptyConfig = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
		var factory = new CardMapFactory(emptyConfig, _catalog);

		var act = () => factory.GenerateReadMap("MOXFIELD");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*No format configurations loaded*");
	}

	[Theory]
	[InlineData("UNKNOWN_FORMAT")]
	public void GenerateWriteMap_ForUnknownFormat_ThrowsWithSupportedList(string deckFormatName)
	{
		var factory = new CardMapFactory(_config, _catalog);
		var act = () => factory.GenerateWriteMap(deckFormatName);
		act.Should().Throw<ArgumentException>()
			.WithMessage("*MOXFIELD*");
	}

	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	[InlineData("MANABOX")]
	[InlineData("TOPDECKED")]
	[InlineData("DECKBOX")]
	public void StreamRoundTripTest(string deckFormatName)
	{
		var handler = new MtgCardCsvHandler(_catalog, _api, _config, deckFormatName);
		var original = new List<PhysicalMtgCard>
		{
			new() { Count = 2, Printing = new Card { Name = "Lightning Bolt", Set = "M11", SetName = "Magic 2011", CollectorNumber = "149" } }
		};

		var stream = new MemoryStream();
		handler.WriteCollectionCsv(original, stream);
		stream.Position = 0;

		var parsed = handler.ParseCollectionCsv(stream).Collection.Cards;

		parsed.Should().HaveCount(1);
		parsed[0].Printing.Name.Should().Be("Lightning Bolt");
		parsed[0].Count.Should().Be(2);
	}

	[Fact]
	public void ParseCollectionCsv_WithCardKingdomAsInput_Throws()
	{
		var handler = new MtgCardCsvHandler(_catalog, _api, _config, "CARDKINGDOM");
		var act = () => handler.ParseCollectionCsv(new MemoryStream("a,b,c\n"u8.ToArray()));
		act.Should().Throw<InvalidOperationException>().WithMessage("*write-only*");
	}
}
