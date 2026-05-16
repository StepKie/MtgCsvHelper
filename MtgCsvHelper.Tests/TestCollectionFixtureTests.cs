namespace MtgCsvHelper.Tests;

// Fixture suffixes wired to theories in this class:
//   *-reference-collection.csv         → all rows parse, Cards.Count == data-row count
//   *-rejected.csv                     → all rows error, no cards land
//   *-mixed-warnings-and-errors.csv    → Cards.Count + ErrorCount == data-row count
//   *-field-fidelity.csv               → driven by MtgCardCsvHandlerTests (not this class)
//
// Other fixtures in Tests/ (warnings-only.csv, wrong-format-headers.csv,
// blank-and-delimiter-rows.csv) are deliberately not wired — they're manual-testing
// inputs for eyeballing Console / Blazor UX with edge-case CSVs.
[Collection(CatalogCollection.Name)]
public class TestCollectionFixtureTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	const string TestCollectionsRoot = "Resources/SampleCsvs/Tests";

	public static TheoryData<string> RejectedFixtures() => DiscoverFixtures("*-rejected.csv");

	public static TheoryData<string> CorrectFixtures() => DiscoverFixtures("*-reference-collection.csv");

	public static TheoryData<string> MixedFixtures() => DiscoverFixtures("*-mixed-warnings-and-errors.csv");

	static TheoryData<string> DiscoverFixtures(string searchPattern)
	{
		// Surface missing-directory cases with the actionable message rather than letting
		// Directory.EnumerateFiles throw an opaque DirectoryNotFoundException.
		if (!Directory.Exists(TestCollectionsRoot))
		{
			throw new InvalidOperationException(
				$"Fixture root '{TestCollectionsRoot}' does not exist. Check CopyToOutputDirectory in the .csproj.");
		}

		var data = new TheoryData<string>();
		foreach (var path in Directory.EnumerateFiles(TestCollectionsRoot, searchPattern, SearchOption.TopDirectoryOnly))
		{
			data.Add(Path.GetFileName(path));
		}

		// Empty TheoryData silently skips the entire theory — turn that into a hard failure
		// so a broken CopyToOutputDirectory / wrong CWD is detected on a fresh CI agent.
		if (data.Count == 0)
		{
			throw new InvalidOperationException(
				$"No '{searchPattern}' fixtures found under '{TestCollectionsRoot}'. Check CopyToOutputDirectory in the .csproj.");
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

		var expectedRows = CountDataRows(filename);

		result.ErrorCount.Should().Be(0, $"reference-collection rows are real exports and should round-trip cleanly. Issues: {string.Join("; ", result.Issues.Select(i => i.Reason))}");
		result.Collection.Cards.Count.Should().Be(expectedRows, $"every data row in {filename} should produce exactly one card (catches silent drops via Warning-severity paths)");
	}

	[Theory]
	[MemberData(nameof(MixedFixtures))]
	public void Mixed_NoSilentSwallow_CardsPlusErrorsEqualDataRows(string filename)
	{
		var format = FormatFromFilename(filename);
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, format);

		var result = handler.ParseCollectionCsv(Path.Combine(TestCollectionsRoot, filename));

		var expectedRows = CountDataRows(filename);

		(result.Collection.Cards.Count + result.ErrorCount).Should().Be(expectedRows,
			$"every data row in {filename} must end up either as a card or as an error — anything in between is a silent swallow");
	}

	// Counts CSV data rows the parser would see: drops blank lines, the optional Excel "sep=..."
	// directive, and the header. Assumes one CSV row per text line — would over-count if a future
	// fixture has a quoted cell with an embedded newline (CsvHelper would parse it as one row).
	static int CountDataRows(string filename) =>
		File.ReadAllLines(Path.Combine(TestCollectionsRoot, filename))
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.SkipWhile(line => line.TrimStart('"').StartsWith("sep=", StringComparison.OrdinalIgnoreCase))
			.Skip(1) // header
			.Count();

	static string FormatFromFilename(string filename)
	{
		// "moxfield-foil-rejected.csv" -> "MOXFIELD"
		// "manabox-reference-collection.csv" -> "MANABOX"
		var stem = Path.GetFileNameWithoutExtension(filename);
		var firstDash = stem.IndexOf('-');
		if (firstDash < 0)
		{
			throw new InvalidOperationException($"Fixture filename '{filename}' does not follow the '<format>-<suffix>.csv' convention.");
		}

		return stem[..firstDash].ToUpperInvariant();
	}
}
