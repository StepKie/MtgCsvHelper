using MtgCsvHelper.Services;

namespace MtgCsvHelper.Tests;

public class ScryfallApiTests
{
	private readonly ScryfallApi _scryfallApi = new();

	[Fact]
	public void DownloadSetsTest()
	{
		var sets = _scryfallApi.GetSets();
		sets.Should().NotBeNullOrEmpty();
		sets.Count().Should().BeGreaterThan(800);
	}
}
