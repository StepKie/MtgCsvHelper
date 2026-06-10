namespace MtgCsvHelper.Tests;

// Fixture suffixes wired to theories in this class:
//   *-real-export.csv                  → all rows parse, Cards.Count == data-row count
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

	public static TheoryData<string> CorrectFixtures() => DiscoverFixtures("*-real-export.csv");

	public static TheoryData<string> MixedFixtures() => DiscoverFixtures("*-mixed-warnings-and-errors.csv");

	static TheoryData<string> DiscoverFixtures(string searchPattern)
	{
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

	/// <summary>
	/// Fixtures with documented site-specific divergence from Scryfall. The listed error count is
	/// expected; the mixed-assertion still forbids silent drops (cards + errors == data rows).
	/// Deckbox's remaining errors are collector-number divergences in legacy sets — it numbers old
	/// reprint products (The List, Alpha, Alliances, Ultimate Box Toppers) differently from Scryfall;
	/// its set-name and edition-code aliasing (EX_NN tokens, PP_NEO promos, 1E/AL/PLIST codes) is handled.
	/// </summary>
	static readonly IReadOnlyDictionary<string, (int ExpectedErrors, string Divergence)> KnownDivergence = new Dictionary<string, (int, string)>
	{
		["deckbox-real-export.csv"] = (ExpectedErrors: 5, Divergence: "legacy-set collector-number divergences"),
	};

	[Theory]
	[MemberData(nameof(CorrectFixtures))]
	public void ReferenceCollection_AllRowsParseAsCards_NoErrors(string filename)
	{
		var format = FormatFromFilename(filename);
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, format);

		var result = handler.ParseCollectionCsv(Path.Combine(TestCollectionsRoot, filename));

		var expectedRows = CsvFixture.CountDataRows(Path.Combine(TestCollectionsRoot, filename));
		var hasKnown = KnownDivergence.TryGetValue(filename, out var known);
		var expectedErrors = hasKnown ? known.ExpectedErrors : 0;
		var because = hasKnown
			? $"{filename} has {known.ExpectedErrors} known site-specific divergences ({known.Divergence})"
			: $"these fixtures are real site exports and should round-trip cleanly. Issues: {string.Join("; ", result.Issues.Select(i => i.Reason))}";

		result.ErrorCount.Should().Be(expectedErrors, because);
		(result.Collection.Cards.Count + result.ErrorCount).Should().Be(expectedRows,
			$"every data row in {filename} should end up as a card or an error — anything in between is a silent swallow");
	}

	[Theory]
	[MemberData(nameof(MixedFixtures))]
	public void Mixed_NoSilentSwallow_CardsPlusErrorsEqualDataRows(string filename)
	{
		var format = FormatFromFilename(filename);
		var handler = new MtgCardCsvHandler(_catalog, _resolver, _config, format);

		var result = handler.ParseCollectionCsv(Path.Combine(TestCollectionsRoot, filename));

		var expectedRows = CsvFixture.CountDataRows(Path.Combine(TestCollectionsRoot, filename));

		(result.Collection.Cards.Count + result.ErrorCount).Should().Be(expectedRows,
			$"every data row in {filename} must end up either as a card or as an error — anything in between is a silent swallow");
	}

	static string FormatFromFilename(string filename)
	{
		// "moxfield-foil-rejected.csv" -> "MOXFIELD"
		// "manabox-real-export.csv" -> "MANABOX"
		var stem = Path.GetFileNameWithoutExtension(filename);
		var firstDash = stem.IndexOf('-');
		if (firstDash < 0)
		{
			throw new InvalidOperationException($"Fixture filename '{filename}' does not follow the '<format>-<suffix>.csv' convention.");
		}

		return stem[..firstDash].ToUpperInvariant();
	}
}
