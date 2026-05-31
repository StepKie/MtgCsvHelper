namespace MtgCsvHelper.Tests;

public class ImportErrorReportTests
{
	static readonly DateTime When = new(2026, 5, 31, 12, 0, 0);

	static ImportIssue Error(int row, string reason, string raw) =>
		new(IssueSeverity.Error, row, reason, RawContent: raw);

	static string BodyOf((string IssueUrl, bool Trimmed) report) => Uri.UnescapeDataString(report.IssueUrl);

	[Fact]
	public void ReasonHistogram_NormalizesCollectorNumbers_SoSameSetAggregates()
	{
		var errors = new[]
		{
			Error(1, "No printing at GK2_AZORIU #2 in Scryfall data", "x"),
			Error(2, "No printing at GK2_AZORIU #3 in Scryfall data", "x"),
			Error(3, "Invalid value 'Surge Foil' for column 'Foil'", "x"),
		};

		var histogram = ImportErrorReport.ReasonHistogram(errors, maxReasons: 20);

		histogram.Should().Contain("2× No printing at GK2_AZORIU #N in Scryfall data");
		histogram.Should().Contain("1× Invalid value 'Surge Foil' for column 'Foil'");
	}

	[Fact]
	public void ReasonHistogram_CapsAtMaxReasons_WithOverflowLine()
	{
		var errors = Enumerable.Range(0, 25)
			.Select(i => Error(i, $"Invalid value 'V{i}' for column 'Foil'", "x"))
			.ToList();

		var histogram = ImportErrorReport.ReasonHistogram(errors, maxReasons: 20);

		histogram.Should().Contain("… and 5 more distinct reasons");
		histogram.Split('\n').Should().HaveCount(21); // 20 reasons + overflow line
	}

	[Fact]
	public void Build_SmallReport_EmbedsEveryRowInline_AndIsNotTrimmed()
	{
		var errors = new[] { Error(1, "No printing at M11 #9999 in Scryfall data", "1,Lightning Bolt,M11,9999") };

		var report = ImportErrorReport.Build("DRAGONSHIELD", "MOXFIELD", When, importedRows: 10, errors);
		var body = BodyOf(report);

		report.Trimmed.Should().BeFalse();
		body.Should().Contain("## Import errors");
		body.Should().Contain("- **Input format**: DRAGONSHIELD");
		body.Should().Contain("- **Errors (rows skipped)**: 1");
		body.Should().Contain("```csv");
		body.Should().Contain("1,Lightning Bolt,M11,9999");
		body.Should().NotContain("was downloaded"); // no attach-fallback note when nothing was trimmed
	}

	[Fact]
	public void Build_TitleUsesFriendlyFormatNames()
	{
		var report = ImportErrorReport.Build("DRAGONSHIELD", "MOXFIELD", When, 1,
			[Error(1, "No printing at M11 #9999 in Scryfall data", "x")]);

		report.IssueUrl.Should().Contain(Uri.EscapeDataString("Import errors: Dragon Shield → Moxfield"));
	}

	[Fact]
	public void Build_LargeReport_TrimsRows_FlagsTruncation_AndStaysUnderUrlCap()
	{
		const int maxUrl = 7500;
		var errors = Enumerable.Range(0, 500)
			.Select(i => Error(i, $"No printing at SET{i} #{i} in Scryfall data",
				$"{i},Some Reasonably Long Card Name Number {i},SET{i},{i}"))
			.ToList();

		var report = ImportErrorReport.Build("DRAGONSHIELD", "MOXFIELD", When, 0, errors, maxUrlLength: maxUrl);
		var body = BodyOf(report);

		report.Trimmed.Should().BeTrue();
		(report.IssueUrl.Length <= maxUrl).Should().BeTrue("the prefilled URL must stay under GitHub's length cap");
		body.Should().Contain("was downloaded"); // attach-fallback note present when trimmed
		body.Should().Contain("### Error reasons"); // the histogram always survives trimming
	}

	[Fact]
	public void BuildCsv_HasHeader_AndQuotesEmbeddedCommasAndQuotes()
	{
		var csv = ImportErrorReport.BuildCsv([Error(5, "Reason with \"quotes\"", "5,\"Name, with comma\",SET,5")]);

		csv.Should().StartWith("RowNumber,RawContent,Reason");
		csv.Should().Contain("\"\"quotes\"\""); // inner quotes doubled
	}
}
