using System.Text;
using System.Text.RegularExpressions;
using MtgCsvHelper.Models;

namespace MtgCsvHelper;

/// <summary>
/// Builds the pre-filled GitHub "new issue" URL for an import that produced errors. The error data
/// is embedded inline — a reason histogram plus the raw rows — so the reporter doesn't have to
/// attach a file. Rows are dropped from the end until the URL fits under GitHub's length cap; when
/// that happens <c>Trimmed</c> is true and the caller should also offer the full CSV download.
/// </summary>
public static class ImportErrorReport
{
	const int DefaultMaxUrlLength = 7500;
	const int DefaultMaxReasons = 20;

	/// <summary>The error rows as a CSV — also offered to the user as a downloadable attachment.</summary>
	public static string BuildCsv(IReadOnlyList<ImportIssue> errors)
	{
		var sb = new StringBuilder();
		sb.AppendLine("RowNumber,RawContent,Reason");
		foreach (var e in errors)
		{
			sb.AppendLine($"{e.RowNumber},{Csv(e.RawContent)},{Csv(e.Reason)}");
		}

		return sb.ToString();

		static string Csv(string? value) => value is null ? "" : $"\"{value.Replace("\"", "\"\"")}\"";
	}

	public static (string IssueUrl, bool Trimmed) Build(
		string inputFormat,
		string outputFormat,
		DateTime importedAt,
		int importedRows,
		IReadOnlyList<ImportIssue> issues,
		int maxUrlLength = DefaultMaxUrlLength,
		int maxReasons = DefaultMaxReasons)
	{
		var errors = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
		var warningCount = issues.Count(i => i.Severity == IssueSeverity.Warning);
		var reasons = ReasonHistogram(errors, maxReasons);

		var csv = BuildCsv(errors).Replace("\r", "").TrimEnd('\n').Split('\n');
		var header = csv[0];
		var rows = csv.Skip(1).ToList();

		// Embed every row, then drop from the end until the URL fits. The summary and reason
		// histogram always fit, so the worst case is zero embedded rows.
		var shown = rows.Count;
		while (shown > 0 && Url(Compose(shown)).Length > maxUrlLength) { shown--; }

		return (Url(Compose(shown)), Trimmed: shown < rows.Count);

		string Compose(int rowCount) => string.Join("\n", new[]
		{
			"## Import errors",
			"",
			$"- **Input format**: {inputFormat}",
			$"- **Output format**: {outputFormat}",
			$"- **When**: {importedAt:yyyy-MM-dd HH:mm}",
			$"- **Rows imported successfully**: {importedRows}",
			$"- **Errors (rows skipped)**: {errors.Count}",
			$"- **Warnings (imported with notes)**: {warningCount}",
			"",
			"### Error reasons",
			"",
			reasons,
			"",
			ErrorDataBlock(header, rows, rowCount, inputFormat),
			"### Additional context",
			"",
			"_Describe what you were trying to do — which source CSV, anything unusual about the data._",
		});

		string Url(string body) =>
			"https://github.com/StepKie/MtgCsvHelper/issues/new"
			+ $"?title={Uri.EscapeDataString($"Import errors: {FormatDisplay.For(inputFormat)} → {FormatDisplay.For(outputFormat)}")}"
			+ $"&labels={Uri.EscapeDataString("bug,import-error")}"
			+ $"&body={Uri.EscapeDataString(body)}";
	}

	// Fenced CSV block of the error rows, plus an attach-the-download note when not all rows fit.
	static string ErrorDataBlock(string header, IReadOnlyList<string> rows, int rowCount, string inputFormat)
	{
		var block = $"### Error data\n\n```csv\n{header}\n{string.Join("\n", rows.Take(rowCount))}\n```\n";
		if (rowCount < rows.Count)
		{
			block += $"\n_Showing {rowCount} of {rows.Count} rows. The full `{inputFormat}-import-errors-*.csv` was downloaded — please drag it into this issue for the complete data._\n";
		}

		return block;
	}

	// Distinct error causes, most frequent first. Per-row collector numbers (#2, #3, …) are
	// normalized to "#N" so repeats of the same set/value collapse into one line.
	internal static string ReasonHistogram(IReadOnlyList<ImportIssue> errors, int maxReasons)
	{
		var groups = errors
			.GroupBy(e => Regex.Replace(e.Reason, @"#\d+", "#N"))
			.OrderByDescending(g => g.Count())
			.ToList();

		var listed = string.Join("\n", groups.Take(maxReasons).Select(g => $"- {g.Count()}× {g.Key}"));

		return groups.Count > maxReasons
			? listed + $"\n- … and {groups.Count - maxReasons} more distinct reasons"
			: listed;
	}
}
