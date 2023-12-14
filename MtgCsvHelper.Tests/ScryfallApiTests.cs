namespace MtgCsvHelper.Tests;

public class MtgApiTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public void DownloadSetsTest()
	{
		var sets = _api.GetSets();
		sets.Should().NotBeNullOrEmpty();
		sets.Count().Should().BeGreaterThan(800);
	}

	[Fact]
	public void DownloadDoubleFacedCardsTest()
	{
		var doubleFacedCards = _api.GetDoubleFacedCardNames();
		doubleFacedCards.Should().NotBeNullOrEmpty();
		doubleFacedCards.Count.Should().BeGreaterThan(100);
	}
}
