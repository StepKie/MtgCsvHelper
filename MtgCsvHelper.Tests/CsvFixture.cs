namespace MtgCsvHelper.Tests;

internal static class CsvFixture
{
	/// <summary>
	/// Counts the CSV data rows the parser would see: drops blank lines, the optional Excel
	/// "sep=..." directive, and the header. Assumes one CSV row per text line — would over-count
	/// if a fixture has a quoted cell with an embedded newline (CsvHelper parses that as one row).
	/// </summary>
	public static int CountDataRows(string path) =>
		File.ReadAllLines(path)
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.SkipWhile(line => line.TrimStart('"').StartsWith("sep=", StringComparison.OrdinalIgnoreCase))
			.Skip(1) // header
			.Count();
}
