namespace MtgCsvHelper.Tests;

[Collection(MtgApiCollection.Name)]
public class MtgApiTests(MtgApiFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
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

	[Fact]
	public async Task DownloadTokensTest()
	{
		var tokenCards = await _api.GetTokenCardNamesAsync();
		var tokenNames = tokenCards.Select(c => c.Name).Distinct().ToList();
		tokenNames.Count.Should().BeGreaterThan(100);
	}
}
