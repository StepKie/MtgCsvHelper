namespace MtgCsvHelper.Tests;

/// <summary>
/// Guards the master.csv ground truth and the CANONICAL format it is written in. These pin the two
/// invariants the whole generation pipeline rests on: master.csv parses cleanly, and CANONICAL is
/// lossless (round-trips its own output). If either breaks, every generated reference CSV is suspect.
/// </summary>
[Collection(CatalogCollection.Name)]
public class CanonicalReferenceTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	[Fact]
	public void Master_ParsesWithoutErrors()
	{
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, CanonicalReference.FormatName);
		var result = handler.ParseCollectionCsv(CanonicalReference.MasterCsvPath);

		result.ErrorCount.Should().Be(0, $"master.csv must parse cleanly. Issues: {string.Join("; ", result.Issues.Select(i => i.Reason))}");
		result.Collection.Cards.Should().NotBeEmpty();
	}

	[Fact]
	public void CanonicalFormat_IsLossless_RoundTripsItsOwnOutput()
	{
		var loaded = CanonicalReference.LoadCards(_config, _catalog, _resolver, Currency.USD);

		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, CanonicalReference.FormatName);
		using var stream = new MemoryStream();
		handler.WriteCollectionCsv(loaded, stream);
		stream.Position = 0;
		var reparsed = handler.ParseCollectionCsv(stream).Collection.Cards;

		reparsed.Should().BeEquivalentTo(loaded);
	}
}
