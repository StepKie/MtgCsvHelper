namespace MtgCsvHelper.Models;

public enum IssueSeverity
{
	Warning,
	Error
}

// Issue surfaced while parsing a CSV. Errors mean the row was skipped (data lost);
// warnings mean the card was imported but with degraded fidelity (e.g. set lookup miss).
public record ImportIssue(
	IssueSeverity Severity,
	int RowNumber,
	string Reason,
	string? CardName = null,
	string? RawContent = null);
