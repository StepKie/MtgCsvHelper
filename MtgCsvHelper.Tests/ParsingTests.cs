using Microsoft.Extensions.Configuration;

namespace MtgCsvHelper.Tests;

public class MtgCardCsvHandlerTests
{

	readonly List<PhysicalMtgCard> _referenceCardSet = new()
	{
		new PhysicalMtgCard
		{
			Count = 1,
			Condition = CardCondition.NEAR_MINT,
			Foil = false,
			Printing = new Printing
			{
				Card = new MtgCard { Name = "Ambitious Farmhand // Seasoned Cathar" },
				IdInSet = "2",
				Set = new Set { Code = "MID", FullName = "Innistrad: Midnight Hunt" },
			},
		},
	};

	[Theory]
	[InlineData("DRAGONSHIELD")]
	[InlineData("MOXFIELD")]
	[InlineData("DECKBOX")]
	public void ParseCollectionCsv_WithValidInput_ParsesCards(string deckFormatName)
	{
		var newCard = _referenceCardSet.First() with { Condition = CardCondition.EXCELLENT };
		// Arrange
		var csvFilePath = "path/to/input.csv";
		var format = new DeckFormat(new ConfigurationBuilder().Build(), deckFormatName);
		var expectedCards = _referenceCardSet;

		// Act
		var cards = new MtgCardCsvHandler(format).ParseCollectionCsv(csvFilePath);

		// Assert
		cards.Should().BeEquivalentTo(expectedCards);
	}

	[Fact()]
	public void WriteCollectionCsvTest()
	{
		Assert.True(false, "This test needs an implementation");
	}
}
