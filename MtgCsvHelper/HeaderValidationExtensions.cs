using CsvHelper;

namespace MtgCsvHelper;

public static class HeaderValidationExtensions
{
	/// <summary>The distinct column names a <see cref="HeaderValidationException"/> reports as missing.</summary>
	public static IReadOnlyList<string> MissingColumns(this HeaderValidationException hex) =>
		hex.InvalidHeaders.SelectMany(h => h.Names).Distinct().ToList();
}
