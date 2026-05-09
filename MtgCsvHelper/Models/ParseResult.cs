namespace MtgCsvHelper.Models;

// Result of parsing a CSV: the resulting Collection plus any per-row issues collected.
// Invariant: cards.Count + ErrorCount == data rows in the CSV (excluding empty/blank lines).
public record ParseResult(Collection Collection, IReadOnlyList<ImportIssue> Issues)
{
	public int ErrorCount => Issues.Count(i => i.Severity == IssueSeverity.Error);
	public int WarningCount => Issues.Count(i => i.Severity == IssueSeverity.Warning);
}
