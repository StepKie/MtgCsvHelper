namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class TestCollectionFixtureTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	const string TestCollectionsRoot = "Resources/SampleCsvs/Tests";

	public static TheoryData<string> RejectedFixtures()
	{
		var data = new TheoryData<string>();
		foreach (var path in Directory.EnumerateFiles(TestCollectionsRoot, "*-rejected.csv", SearchOption.TopDirectoryOnly))
		{
			data.Add(Path.GetFileName(path));
		}

		return data;
	}

	public static TheoryData<string> CorrectFixtures()
	{
		var data = new TheoryData<string>();
		foreach (var path in Directory.EnumerateFiles(TestCollectionsRoot, "*-reference-collection.csv", SearchOption.TopDirectoryOnly))
		{
			data.Add(Path.GetFileName(path));
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(RejectedFixtures))]
	public void Rejected_AllRowsProduceErrors_NoCardsImported(string filename)
	{
		var format = FormatFromFilename(filename);
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, format);

		var result = handler.ParseCollectionCsv(Path.Combine(TestCollectionsRoot, filename));

		result.Collection.Cards.Should().BeEmpty($"every row in {filename} is intentionally invalid");
		result.Issues.Should().NotBeEmpty();
		result.Issues.Should().AllSatisfy(i => i.Severity.Should().Be(IssueSeverity.Error));
	}

	[Theory]
	[MemberData(nameof(CorrectFixtures))]
	public void ReferenceCollection_AllRowsParseAsCards_NoErrors(string filename)
	{
		var format = FormatFromFilename(filename);
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, format);

		var result = handler.ParseCollectionCsv(Path.Combine(TestCollectionsRoot, filename));

		result.Collection.Cards.Should().NotBeEmpty();
		result.ErrorCount.Should().Be(0, $"reference-collection rows are real exports and should round-trip cleanly. Issues: {string.Join("; ", result.Issues.Select(i => i.Reason))}");
	}

	static string FormatFromFilename(string filename)
	{
		// "moxfield-foil-rejected.csv" -> "MOXFIELD"
		// "manabox-reference-collection.csv" -> "MANABOX"
		var stem = Path.GetFileNameWithoutExtension(filename);
		var firstDash = stem.IndexOf('-');

		return stem[..firstDash].ToUpperInvariant();
	}
}
