using System.Text;

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
			.SkipWhile(IsSepDirective)
			.Skip(1) // header
			.Count();

	/// <summary>True for the optional (possibly quoted) Excel "sep=..." directive some exports prepend.</summary>
	public static bool IsSepDirective(string line) =>
		line.TrimStart('"').StartsWith("sep=", StringComparison.OrdinalIgnoreCase);

	public static MemoryStream CsvStream(string csv) => new(Encoding.UTF8.GetBytes(csv));

	public static string WriteToString(MtgCardCsvHandler handler, IList<PhysicalMtgCard> cards)
	{
		using var stream = new MemoryStream();
		handler.WriteCollectionCsv(cards, stream);

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	/// <summary>Format from the fixture naming convention: "moxfield-real-export.csv" → "MOXFIELD".</summary>
	public static string FormatFromFilename(string filename)
	{
		var stem = Path.GetFileNameWithoutExtension(filename);
		var firstDash = stem.IndexOf('-');
		if (firstDash < 0)
		{
			throw new InvalidOperationException($"Fixture filename '{filename}' does not follow the '<format>-<suffix>.csv' convention.");
		}

		return stem[..firstDash].ToUpperInvariant();
	}
}
